#pragma once

struct PrintJob 
{
	std::wstring PrinterName;
	std::wstring DocumentName;
	std::wstring Datatype;

	HRESULT ToXml(std::wstring& xml);
};