#pragma once

#define INVALID_JOB_ID (-1)

class EsVpPort
{
private:
	int JobId;
	std::wstring PrinterName;
	std::wstring DocumentName;
	std::wstring Datatype;
	std::wstring TempFileName;

	HANDLE hPrinter;
	HANDLE hTempFile;

	void Cleanup();

public:
	EsVpPort(std::wstring portName);
	~EsVpPort();

	bool TryStartDoc(std::wstring printerName, int jobId, DOC_INFO_1* pDocInfo);
	bool WritePort(LPBYTE  pBuffer, DWORD cbBuf, _Out_ LPDWORD pcbWritten);
	bool EndDoc();
	bool ClosePort();
};

