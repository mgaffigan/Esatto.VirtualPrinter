#pragma once

typedef wil::unique_any<HANDLE, decltype(&::ClosePrinter), ::ClosePrinter> unique_hprinter;

#define INVALID_JOB_ID (-1)

class EsVpPort
{
private:
	int JobId;
	std::wstring PrinterName;
	std::wstring DocumentName;
	std::wstring Datatype;
	std::wstring TempFileName;
	std::wstring TargetExe;
	std::wstring TargetArguments;

	unique_hprinter hPrinter;
	wil::unique_hfile hTempFile;

	HRESULT EndDocInternal();

public:
	EsVpPort(std::wstring portName);
	~EsVpPort();

	HRESULT TryStartDoc(std::wstring printerName, int jobId, DOC_INFO_1* pDocInfo);
	HRESULT WritePort(LPBYTE  pBuffer, DWORD cbBuf, _Out_ LPDWORD pcbWritten);
	HRESULT EndDoc();
};

