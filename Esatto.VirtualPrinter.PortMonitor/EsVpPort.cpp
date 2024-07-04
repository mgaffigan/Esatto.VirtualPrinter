#include "stdafx.h"
#include <wil/registry.h>
#include <wil/token_helpers.h>

EsVpPort::EsVpPort(std::wstring portName)
{
	this->JobId = INVALID_JOB_ID;
}

EsVpPort::~EsVpPort()
{
}

HRESULT EsVpPort::TryStartDoc(std::wstring printerName, int jobId, DOC_INFO_1* pDocInfo)
{
	RETURN_HR_IF_MSG(HRESULT_FROM_WIN32(ERROR_ALREADY_EXISTS), this->JobId != INVALID_JOB_ID, "Job already started");

	// Get the target of the printer
	{
		std::wstring printerKeyName = L"SOFTWARE\\Esatto\\VirtualPrinter\\Printers\\";
		printerKeyName += printerName;
		wil::unique_hkey hPrinterKey;
		RETURN_IF_FAILED_MSG(wil::reg::open_unique_key_nothrow(HKEY_LOCAL_MACHINE, printerKeyName.c_str(), hPrinterKey, wil::reg::key_access::read), "Printer not registered with ESVP");
		wil::unique_bstr temp;
		RETURN_IF_FAILED_MSG(wil::reg::get_value_string_nothrow(hPrinterKey.get(), L"TargetExe", temp), "Could not get TargetExe");
		this->TargetExe = temp.get();
		temp.reset();
		auto args_hr = wil::reg::get_value_string_nothrow(hPrinterKey.get(), L"TargetArgs", temp);
		if (args_hr == HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND))
		{
			this->TargetArguments = L"\"%1\"";
		}
		else
		{
			RETURN_IF_FAILED_MSG(args_hr, "Could not get TargetArgs");
			this->TargetArguments = temp.get();
		}
	}

	// Copy job info
	this->JobId = jobId;
	this->PrinterName = printerName;
	this->DocumentName = pDocInfo->pDocName == nullptr ? L"" : pDocInfo->pDocName;
	this->Datatype = pDocInfo->pDatatype == nullptr ? L"" : pDocInfo->pDatatype;

	// Create a spool file
	wchar_t tempPath[MAX_PATH];
	RETURN_LAST_ERROR_IF_MSG(GetTempPathW(ARRAYSIZE(tempPath), tempPath) == 0, "Could not get temp file directory");
	wchar_t tempFileName[MAX_PATH];
	RETURN_LAST_ERROR_IF_MSG(GetTempFileNameW(tempPath, L"xps", 0, tempFileName) == 0, "Could not get temp file name");
	this->TempFileName = tempFileName;

	this->hTempFile = wil::unique_hfile(CreateFile(tempFileName, GENERIC_READ | GENERIC_WRITE, 0, NULL, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, NULL));
	RETURN_LAST_ERROR_IF_MSG(!this->hTempFile.is_valid(), "Could not create spool file");

	RETURN_LAST_ERROR_IF_MSG(OpenPrinter((LPWSTR)printerName.c_str(), this->hPrinter.put(), NULL), "Could not open printer");

	return S_OK;
}

HRESULT EsVpPort::WritePort(LPBYTE pBuffer, DWORD cbBuf, _Out_ LPDWORD pcbWritten)
{
	UNREFERENCED_PARAMETER(pBuffer);
	RETURN_HR_IF_MSG(HRESULT_FROM_WIN32(ERROR_INVALID_HANDLE), this->JobId == INVALID_JOB_ID, "Job not started");

	RETURN_LAST_ERROR_IF_MSG(WriteFile(this->hTempFile.get(), pBuffer, cbBuf, pcbWritten, NULL) == 0, "Could not write to spool file");

	return S_OK;
}

HRESULT GetUserThreadTokenInfo(DWORD *pSessionID, std::wstring* pSid)
{
	*pSessionID = 0;

	wil::unique_handle hToken;
	RETURN_LAST_ERROR_IF_MSG(!OpenThreadToken(GetCurrentThread(), MAXIMUM_ALLOWED, FALSE, hToken.put()), "Could not open thread token");
	
	DWORD _1;
	RETURN_LAST_ERROR_IF_MSG(!GetTokenInformation(hToken.get(), TokenSessionId, pSessionID, 4, &_1), "Could not get user session ID");

	wil::unique_tokeninfo_ptr<TOKEN_USER> user;
	RETURN_IF_FAILED(wil::get_token_information_nothrow(user, hToken.get()));

	wil::unique_hlocal_string pSidString;
	RETURN_LAST_ERROR_IF_MSG(!ConvertSidToStringSidW(user.get()->User.Sid, pSidString.put()), "Could not format SID");
	*pSid = std::wstring(pSidString.get());

	return S_OK;
}

HRESULT EsVpPort::EndDocInternal()
{
	// send to the handler
	// close the file so that it is flushed and sharable
	this->hTempFile.reset();

	// send to server
	PrintJob printJob;
	printJob.PrinterName = this->PrinterName;
	printJob.DocumentName = this->DocumentName;
	printJob.SpoolFilePath = this->TempFileName;

	// save to file
	std::wstring jobFile = printJob.SpoolFilePath + L".job";
	RETURN_IF_FAILED_MSG(printJob.SaveTo(jobFile), "Could not write job file");

	// app information
	std::wstring search = L"%1";
	auto substitution = this->TargetArguments.find(search);
	this->TargetArguments.replace(substitution, search.size(), jobFile);
	STARTUPINFOW si = { sizeof(si) };
	si.lpDesktop = const_cast<wchar_t*>(L"WinSta0\\Default");

	// invoke in session
	wil::unique_handle hToken;
	RETURN_LAST_ERROR_IF_MSG(!OpenThreadToken(GetCurrentThread(), MAXIMUM_ALLOWED, FALSE, hToken.put()), "Could not open thread token");
	wil::unique_process_information hProcess;
	RETURN_LAST_ERROR_IF_MSG(!CreateProcessAsUserW(
		hToken.get(), this->TargetExe.c_str(), const_cast<LPWSTR>(this->TargetArguments.c_str()), 
		/* _In_opt_ LPSECURITY_ATTRIBUTES lpProcessAttributes */ NULL, 
		/* _In_opt_ LPSECURITY_ATTRIBUTES lpThreadAttributes */ NULL,
		/* _In_ BOOL bInheritHandles */ FALSE, 
		/* _In_ DWORD dwCreationFlags */ 0, 
		/* _In_opt_ LPVOID lpEnvironment */ NULL, 
		/* _In_opt_ LPCWSTR lpCurrentDirectory */ NULL, 
		/* _In_ LPSTARTUPINFOW lpStartupInfo */ &si, 
		/* _Out_ LPPROCESS_INFORMATION lpProcessInformation */ &hProcess
	), "Could not start handler");

	return S_OK;
}

HRESULT EsVpPort::EndDoc()
{
	RETURN_HR_IF_MSG(HRESULT_FROM_WIN32(ERROR_INVALID_HANDLE), this->JobId == INVALID_JOB_ID, "Job not started");

	auto result = EndDocInternal();
	// We have to end the job regardless of failure
	SetJob(hPrinter.get(), JobId, 0, NULL, JOB_CONTROL_SENT_TO_PRINTER);
	return result;
}
