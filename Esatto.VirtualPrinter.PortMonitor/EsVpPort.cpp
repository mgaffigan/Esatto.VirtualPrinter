#include "stdafx.h"

EsVpPort::EsVpPort(std::wstring portName)
{
	this->JobId = INVALID_JOB_ID;
}


EsVpPort::~EsVpPort()
{
}

void EsVpPort::Cleanup()
{
	if (hPrinter != INVALID_HANDLE_VALUE)
	{
		ClosePrinter(hPrinter);
	}
	if (hTempFile != INVALID_HANDLE_VALUE)
	{
		CloseHandle(hTempFile);
	}

	this->JobId = INVALID_JOB_ID;
	this->hTempFile = INVALID_HANDLE_VALUE;
	this->hPrinter = INVALID_HANDLE_VALUE;
	this->PrinterName.clear();
	this->DocumentName.clear();
	this->Datatype.clear();
	this->TempFileName.clear();
}

bool EsVpPort::TryStartDoc(std::wstring printerName, int jobId, DOC_INFO_1* pDocInfo)
{
	if (this->JobId != INVALID_JOB_ID)
	{
		SetLastError(ERROR_ALREADY_EXISTS);
		return false;
	}

	this->JobId = jobId;
	this->PrinterName = printerName;
	this->DocumentName = pDocInfo->pDocName == nullptr ? L"" : pDocInfo->pDocName;
	this->Datatype = pDocInfo->pDatatype == nullptr ? L"" : pDocInfo->pDatatype;
	this->hTempFile = INVALID_HANDLE_VALUE;
	this->hPrinter = INVALID_HANDLE_VALUE;
	this->TempFileName.clear();

	wchar_t tempPath[MAX_PATH];
	GetTempPath(ARRAYSIZE(tempPath), tempPath);
	wchar_t tempFileName[MAX_PATH];
	if (!GetTempFileNameW(tempPath, L"xps", 0, tempFileName))
	{
		goto fail;
	}

	this->TempFileName = tempFileName;
	this->hTempFile = CreateFile(tempFileName, GENERIC_READ | GENERIC_WRITE, 0, NULL, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, NULL);
	if (hTempFile == INVALID_HANDLE_VALUE)
	{
		goto fail;
	}

	if (!OpenPrinter((LPWSTR)printerName.c_str(), &this->hPrinter, NULL))
	{
		goto fail;
	}

	return true;

fail:
	// grab GetLastError since cleanup may overwrite
	auto error = GetLastError();
	Cleanup();

	SetLastError(error);
	return false;
}

bool EsVpPort::WritePort(LPBYTE pBuffer, DWORD cbBuf, LPDWORD pcbWritten)
{
	UNREFERENCED_PARAMETER(pBuffer);

	if (this->JobId == INVALID_JOB_ID)
	{
		SetLastError(ERROR_INVALID_HANDLE);
		return false;
	}

	return !!WriteFile(this->hTempFile, pBuffer, cbBuf, pcbWritten, NULL);
}

bool GetUserThreadTokenInfo(DWORD *pSessionID, std::wstring* pSid)
{
	*pSessionID = 0;
	*pSid = L"";
	bool result = false;

	HANDLE hToken;
	if (!OpenThreadToken(GetCurrentThread(), MAXIMUM_ALLOWED, FALSE, &hToken))
	{
		return false;
	}

	DWORD cbNeeded;
	if (!GetTokenInformation(hToken, TokenSessionId, pSessionID, 4, &cbNeeded))
	{
		goto cleanup1;
	}

	if (GetTokenInformation(hToken, TokenUser, nullptr, 0, &cbNeeded)
		|| GetLastError() != ERROR_INSUFFICIENT_BUFFER)
	{
		goto cleanup1;
	}

	BYTE* pBuf = new BYTE[cbNeeded];
	if (!GetTokenInformation(hToken, TokenUser, pBuf, cbNeeded, &cbNeeded))
	{
		goto cleanup2;
	}

	wchar_t* pSidString;
	if (!ConvertSidToStringSid(((PTOKEN_USER)pBuf)->User.Sid, &pSidString))
	{
		goto cleanup2;
	}
	*pSid = pSidString;
	LocalFree(pSidString);

	result = true;

cleanup2:
	delete[] pBuf;
cleanup1:
	CloseHandle(hToken);
	return result;
}

bool EsVpPort::EndDoc()
{
	using namespace Esatto_VirtualPrinter_Common;

	PrintJob printJob = {0};
	if (JobId == INVALID_JOB_ID)
	{
		SetLastError(ERROR_INVALID_HANDLE);
		return false;
	}

	// send to the handler
	// close the file so that it is flushed and sharable
	CloseHandle(this->hTempFile);
	this->hTempFile = INVALID_HANDLE_VALUE;

	// send to server
	printJob.PrinterName = SysAllocStringLen(this->PrinterName.data(), (UINT)this->PrinterName.length());
	printJob.JobId = this->JobId;
	printJob.DocumentName = SysAllocStringLen(this->DocumentName.data(), (UINT)this->DocumentName.length());
	printJob.SpoolFilePath = SysAllocStringLen(this->TempFileName.data(), (UINT)this->TempFileName.length());

	// get user info from thread token (via impersonation)
	std::wstring sid;
	DWORD sessionID;
	if (!GetUserThreadTokenInfo(&sessionID, &sid))
	{
		goto cleanup2;
	}
	printJob.SessionId = (long)sessionID;
	printJob.UserSid = SysAllocStringLen(sid.data(), (UINT)sid.length());

	// call COM (with small scope to ensure quick Release)
	try
	{
		IPrintRedirectorPtr pRedirector(NMON_REDIRECTOR_PROGID);
		pRedirector->HandleJob(printJob);
	}
	catch (_com_error &error)
	{
		// no-op
		UNREFERENCED_PARAMETER(error);
	}

cleanup2:
	if (!SetJob(hPrinter, JobId, 0, NULL, JOB_CONTROL_SENT_TO_PRINTER))
	{
		goto cleanup1;
	}

cleanup1:
	Cleanup();
	// SysFreeString is fine with nulls
	SysFreeString(printJob.PrinterName);
	SysFreeString(printJob.UserSid);
	SysFreeString(printJob.DocumentName);
	SysFreeString(printJob.SpoolFilePath);

	return true;
}

bool EsVpPort::ClosePort()
{
	return true;
}
