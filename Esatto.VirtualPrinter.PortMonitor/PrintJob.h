#pragma once

struct PrintJob 
{
	std::wstring PrinterName;
	std::wstring DocumentName;
	std::wstring SpoolFilePath;

	HRESULT SaveTo(std::wstring path);
};