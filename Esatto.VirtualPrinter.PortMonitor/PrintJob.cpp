#include "stdafx.h"
#include <MsXml6.h>
#pragma comment(lib, "msxml6.lib")

#define _T _variant_t

HRESULT PrintJob::ToXml(std::wstring& xml)
{
	wil::com_ptr<IXMLDOMDocument> doc = wil::CoCreateInstance<IXMLDOMDocument>(CLSID_DOMDocument60);

	wil::com_ptr<IXMLDOMNode> root;
	RETURN_IF_FAILED(doc->createNode(_T(NODE_ELEMENT), _bstr_t(L"PrintJob"), _bstr_t(L"urn:esatto:vp4"), root.put()));
	RETURN_IF_FAILED(doc->appendChild(root.get(), nullptr));
	wil::com_ptr<IXMLDOMElement> r = root.query<IXMLDOMElement>();

	// This does not handle embedded nuls, but I don't care.
	RETURN_IF_FAILED(r->setAttribute(_bstr_t(L"PrinterName"), _T(PrinterName.c_str())));
	RETURN_IF_FAILED(r->setAttribute(_bstr_t(L"DocumentName"), _T(DocumentName.c_str())));
	RETURN_IF_FAILED(r->setAttribute(_bstr_t(L"DataType"), _T(Datatype.c_str())));

	_bstr_t xmlText;
	RETURN_IF_FAILED(doc->get_xml(xmlText.GetAddress()));
	xml = std::wstring(xmlText, xmlText.length());

	return S_OK;
}