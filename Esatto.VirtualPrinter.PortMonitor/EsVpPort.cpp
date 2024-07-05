#include "stdafx.h"
#include <wil/registry.h>
#include <wil/token_helpers.h>

EsVpPort::EsVpPort(std::wstring portName)
{
}

EsVpPort::~EsVpPort()
{
}

HRESULT EsVpTarget::Load(std::wstring printerName)
{
	std::wstring printerKeyName = L"SOFTWARE\\In Touch Technologies\\Esatto\\Virtual Printer\\Printers\\";
	printerKeyName += printerName;
	wil::unique_hkey hPrinterKey;
	RETURN_IF_FAILED_MSG(wil::reg::open_unique_key_nothrow(HKEY_LOCAL_MACHINE, printerKeyName.c_str(), hPrinterKey, wil::reg::key_access::read), "Printer not registered with ESVP");
	wil::unique_bstr temp;
	RETURN_IF_FAILED_MSG(wil::reg::get_value_string_nothrow(hPrinterKey.get(), L"TargetExe", temp), "Could not get TargetExe");
	this->Exe = temp.get();
	temp.reset();
	auto args_hr = wil::reg::get_value_string_nothrow(hPrinterKey.get(), L"TargetArgs", temp);
	if (args_hr == HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND))
	{
		this->Args = L"\"%1\"";
	}
	else
	{
		RETURN_IF_FAILED_MSG(args_hr, "Could not get TargetArgs");
		this->Args = temp.get();
	}

	return S_OK;
}

EsVpDocument::EsVpDocument(std::wstring printerName, int jobId, DOC_INFO_1* pDocInfo)
{
	this->JobId = jobId;
	this->PrinterName = printerName;
	this->DocumentName = pDocInfo->pDocName == nullptr ? L"" : pDocInfo->pDocName;
	this->Datatype = pDocInfo->pDatatype == nullptr ? L"" : pDocInfo->pDatatype;
}

HRESULT EsVpPort::TryStartDoc(std::wstring printerName, int jobId, DOC_INFO_1* pDocInfo)
{
	RETURN_HR_IF_MSG(HRESULT_FROM_WIN32(ERROR_ALREADY_EXISTS), !!this->current_doc, "Job already started");

	// Copy job info
	std::unique_ptr<EsVpDocument> doc = std::make_unique<EsVpDocument>(printerName, jobId, pDocInfo);

	// Get the target of the printer
	RETURN_IF_FAILED_MSG(doc->Target.Load(printerName), "Could not load target information");

	// Create a spool file
	wchar_t tempPath[MAX_PATH];
	RETURN_LAST_ERROR_IF_MSG(GetTempPathW(ARRAYSIZE(tempPath), tempPath) == 0, "Could not get temp file directory");
	wchar_t tempFileName[MAX_PATH];
	RETURN_LAST_ERROR_IF_MSG(GetTempFileNameW(tempPath, L"esv", 0, tempFileName) == 0, "Could not get temp file name");
	doc->TempFileName = tempFileName;

	doc->hTempFile = wil::unique_hfile(CreateFile(tempFileName, GENERIC_READ | GENERIC_WRITE, 0, NULL, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, NULL));
	RETURN_LAST_ERROR_IF_MSG(!doc->hTempFile.is_valid(), "Could not create spool file");

	RETURN_LAST_ERROR_IF_MSG(!OpenPrinter((LPWSTR)printerName.c_str(), doc->hPrinter.put(), NULL), "Could not open printer");

	// Delay "starting" the job until we are fully constructed
	this->current_doc = std::move(doc);

	return S_OK;
}

HRESULT EsVpPort::WritePort(LPBYTE pBuffer, DWORD cbBuf, _Out_ LPDWORD pcbWritten)
{
	UNREFERENCED_PARAMETER(pBuffer);
	RETURN_HR_IF_MSG(HRESULT_FROM_WIN32(ERROR_INVALID_HANDLE), !this->current_doc, "Job not started");

	RETURN_LAST_ERROR_IF_MSG(WriteFile(this->current_doc->hTempFile.get(), pBuffer, cbBuf, pcbWritten, NULL) == 0, "Could not write to spool file");

	return S_OK;
}

//HRESULT GetUserThreadTokenInfo(DWORD *pSessionID, std::wstring* pSid)
//{
//	*pSessionID = 0;
//
//	wil::unique_handle hToken;
//	RETURN_LAST_ERROR_IF_MSG(!OpenThreadToken(GetCurrentThread(), MAXIMUM_ALLOWED, FALSE, hToken.put()), "Could not open thread token");
//	
//	DWORD _1;
//	RETURN_LAST_ERROR_IF_MSG(!GetTokenInformation(hToken.get(), TokenSessionId, pSessionID, 4, &_1), "Could not get user session ID");
//
//	wil::unique_tokeninfo_ptr<TOKEN_USER> user;
//	RETURN_IF_FAILED(wil::get_token_information_nothrow(user, hToken.get()));
//
//	wil::unique_hlocal_string pSidString;
//	RETURN_LAST_ERROR_IF_MSG(!ConvertSidToStringSidW(user.get()->User.Sid, pSidString.put()), "Could not format SID");
//	*pSid = std::wstring(pSidString.get());
//
//	return S_OK;
//}

PrintJob EsVpDocument::ToPrintJob()
{
	PrintJob printJob;
	printJob.PrinterName = this->PrinterName;
	printJob.DocumentName = this->DocumentName;
	printJob.Datatype = this->Datatype;
	return printJob;
}

void FindReplace(std::wstring& str, const std::wstring& search, const std::wstring& replace)
{
	size_t pos = 0;
	while ((pos = str.find(search, pos)) != std::wstring::npos)
	{
		str.replace(pos, search.length(), replace);
		pos += replace.length();
	}
}

HRESULT EsVpTarget::Invoke(std::wstring jobFileName) 
{
	std::wstring args = Args;
	FindReplace(args, L"%1", jobFileName);
	args = std::wstring(L"\"") + Exe + L"\" " + args;

	STARTUPINFOW si = { sizeof(si) };
	si.lpDesktop = const_cast<wchar_t*>(L"WinSta0\\Default");

	wil::unique_handle hToken;
	RETURN_LAST_ERROR_IF_MSG(!OpenThreadToken(GetCurrentThread(), MAXIMUM_ALLOWED, FALSE, hToken.put()), "Could not open thread token");

	wil::unique_environment_block env;
	RETURN_LAST_ERROR_IF_MSG(!CreateEnvironmentBlock(&env, hToken.get(), FALSE), "Could not create environment block");

	wil::unique_process_information hProcess;
	RETURN_LAST_ERROR_IF_MSG(!CreateProcessAsUserW(
		hToken.get(), this->Exe.c_str(), const_cast<LPWSTR>(args.c_str()),
		/* _In_opt_ LPSECURITY_ATTRIBUTES lpProcessAttributes */ NULL,
		/* _In_opt_ LPSECURITY_ATTRIBUTES lpThreadAttributes */ NULL,
		/* _In_ BOOL bInheritHandles */ FALSE,
		/* _In_ DWORD dwCreationFlags */ CREATE_UNICODE_ENVIRONMENT,
		/* _In_opt_ LPVOID lpEnvironment */ env.get(),
		/* _In_opt_ LPCWSTR lpCurrentDirectory */ NULL,
		/* _In_ LPSTARTUPINFOW lpStartupInfo */ &si,
		/* _Out_ LPPROCESS_INFORMATION lpProcessInformation */ &hProcess
	), "Could not start handler");

	return S_OK;
}

EsVpDocument::~EsVpDocument()
{
	if (hPrinter)
	{
		// We have to end the job regardless of failure
		SetJob(hPrinter.get(), JobId, 0, NULL, JOB_CONTROL_SENT_TO_PRINTER);
	}
}

HRESULT EsVpPort::EndDoc()
{
	RETURN_HR_IF_MSG(HRESULT_FROM_WIN32(ERROR_INVALID_HANDLE), !this->current_doc, "Job not started");

	// Take to local scope to ensure it is cleaned up regardless of failure
	std::unique_ptr<EsVpDocument> doc = std::move(this->current_doc);

	// save job file
	auto printJob = doc->ToPrintJob();
	std::wstring xml;
	RETURN_IF_FAILED_MSG(printJob.ToXml(xml), "Could not serialize job");

	DWORD cbXml = (DWORD)(sizeof(wchar_t) * xml.length()), cbWritten = 0;
	RETURN_LAST_ERROR_IF_MSG(WriteFile(doc->hTempFile.get(), (LPCVOID)xml.c_str(), cbXml, &cbWritten, NULL) == 0, "Could not write to spool file");
	RETURN_HR_IF_MSG(HRESULT_FROM_WIN32(ERROR_HANDLE_EOF), cbWritten != cbXml, "Could not write all of the job file");

	// We're intentionally neglecting the possibility of a big-endian machine here.
	// Justification: I don't care.
	cbWritten = 0;
	RETURN_LAST_ERROR_IF_MSG(WriteFile(doc->hTempFile.get(), (LPCVOID)&cbXml, sizeof(DWORD), &cbWritten, NULL) == 0, "Could not write to spool file");
	RETURN_HR_IF_MSG(HRESULT_FROM_WIN32(ERROR_HANDLE_EOF), cbWritten != sizeof(DWORD), "Could not write all of the job file");

	// close the file so that it is flushed and sharable
	doc->hTempFile.reset();

	// invoke in session
	doc->Target.Invoke(doc->TempFileName);

	return S_OK;
}
