#pragma once

typedef wil::unique_any<HANDLE, decltype(&::ClosePrinter), ::ClosePrinter> unique_hprinter;

#define INVALID_JOB_ID (-1)

struct EsVpDocument;

class EsVpPort
{
private:
	std::unique_ptr<EsVpDocument> current_doc;

public:
	EsVpPort(std::wstring portName);
	~EsVpPort();

	HRESULT TryStartDoc(std::wstring printerName, int jobId, DOC_INFO_1* pDocInfo);
	HRESULT WritePort(LPBYTE  pBuffer, DWORD cbBuf, _Out_ LPDWORD pcbWritten);
	HRESULT EndDoc();
};

struct EsVpTarget
{
	std::wstring Exe;
	std::wstring Args;

	HRESULT Load(std::wstring printerName);

	HRESULT Invoke(std::wstring jobFileName);
};

struct EsVpDocument
{
	int JobId;
	std::wstring PrinterName;
	std::wstring DocumentName;
	std::wstring Datatype;
	std::wstring TempFileName;
	EsVpTarget Target;

	unique_hprinter hPrinter;
	wil::unique_hfile hTempFile;

	EsVpDocument(std::wstring printerName, int jobId, DOC_INFO_1* pDocInfo);
	~EsVpDocument();

	PrintJob ToPrintJob();
};