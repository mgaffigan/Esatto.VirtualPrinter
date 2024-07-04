#include "stdafx.h"
#include <MsXml6.h>
#pragma comment(lib, "msxml6.lib")

#define _T _variant_t

HRESULT PrintJob::SaveTo(std::wstring path)
{
	wil::com_ptr<IXMLDOMDocument> doc = wil::CoCreateInstance<IXMLDOMDocument>(CLSID_DOMDocument60);

	wil::com_ptr<IXMLDOMNode> root;
	RETURN_IF_FAILED(doc->createNode(_T(NODE_ELEMENT), _bstr_t(L"PrintJob"), _bstr_t(L"urn:esatto:vp4"), root.put()));
	RETURN_IF_FAILED(doc->appendChild(root.get(), nullptr));
	wil::com_ptr<IXMLDOMElement> r = root.query<IXMLDOMElement>();

	// This does not handle embedded nuls, but I don't care.
	RETURN_IF_FAILED(r->setAttribute(_bstr_t(L"PrinterName"), _T(PrinterName.c_str())));
	RETURN_IF_FAILED(r->setAttribute(_bstr_t(L"DocumentName"), _T(DocumentName.c_str())));
	RETURN_IF_FAILED(r->setAttribute(_bstr_t(L"SpoolFilePath"), _T(SpoolFilePath.c_str())));

	RETURN_IF_FAILED(doc->save(_T(path.c_str())));

	return S_OK;
}