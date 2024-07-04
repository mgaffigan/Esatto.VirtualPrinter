#include "stdafx.h"
#include "Entrypoint.h"

MONITOR2 g_dispatchTable;

void InitializeDispatchTable(PMONITOR2 dt);

LPMONITOR2 WINAPI InitializePrintMonitor2(
	_In_    PMONITORINIT pMonitorInit,
	_Out_   PHANDLE phMonitor)
{
	UNREFERENCED_PARAMETER(pMonitorInit);

	auto newMonitor = new EsVpPortMon();
	*phMonitor = reinterpret_cast<PHANDLE>(newMonitor);

	g_dispatchTable = { 0 };
	InitializeDispatchTable(&g_dispatchTable);
	return &g_dispatchTable;
}

_Success_(return != FALSE)
BOOL WINAPI LcmEnumPorts(
	_In_ HANDLE  hMonitor,
	_In_opt_ LPWSTR pName,
	DWORD   level,
	_Out_writes_bytes_opt_(cbBuf) LPBYTE  pPorts,
	DWORD   cbBuf,
	_Out_ LPDWORD pcbNeeded,
	_Out_ LPDWORD pcReturned)
{
	// pName is server name for cross server enumeration.
	UNREFERENCED_PARAMETER(pName);

	if (!hMonitor || !pPorts)
	{
		SetLastError(ERROR_INVALID_PARAMETER);
		*pcbNeeded = 0;
		*pcReturned = 0;
		return FALSE;
	}

	if (level != 1 && level != 2)
	{
		SetLastError(ERROR_INVALID_LEVEL);
		*pcbNeeded = 0;
		*pcReturned = 0;
		return FALSE;
	}

	auto pEsVpPortMon = reinterpret_cast<EsVpPortMon*>(hMonitor);
	auto portInfos = pEsVpPortMon->EnumPorts();

	// tally memory
	size_t cbReq = 0;
	for (auto &cPort : portInfos)
	{
		if (level == 1)
		{
			cbReq += sizeof(PORT_INFO_1);
		}
		else if (level == 2)
		{
			cbReq += sizeof(PORT_INFO_2);
		}

		cbReq += cPort.GetRequiredLength();
	}

	if (cbReq > cbBuf)
	{
		*pcbNeeded = (DWORD)cbReq;
		*pcReturned = 0;
		SetLastError(ERROR_INSUFFICIENT_BUFFER);
		return FALSE;
	}

	auto pNextString = pPorts + cbBuf;
	if (level == 1)
	{
		auto pNextStruct = reinterpret_cast<PORT_INFO_1*>(pPorts);
		for (auto &cPort : portInfos)
		{
			size_t cbPortName = (cPort.PortName.length() + 1) * sizeof(wchar_t);
			pNextString -= cbPortName; LPWSTR pPortName = reinterpret_cast<LPWSTR>(pNextString);
			std::memcpy(pPortName, cPort.PortName.c_str(), cbPortName);

			pNextStruct->pName = pPortName;
			pNextStruct += 1;
		}
	}
	else if (level == 2)
	{
		memset(pPorts, 0, cbReq);
		auto pNextStruct = reinterpret_cast<PORT_INFO_2*>(pPorts);
		for (auto &cPort : portInfos)
		{
			size_t cbPortName = (cPort.PortName.length() + 1) * sizeof(wchar_t);
			pNextString -= cbPortName; LPWSTR pPortName = reinterpret_cast<LPWSTR>(pNextString);
			std::memcpy(pPortName, cPort.PortName.c_str(), cbPortName);

			size_t cbMonitorName = (cPort.MonitorName.length() + 1) * sizeof(wchar_t);
			pNextString -= cbMonitorName; LPWSTR pMonitorName = reinterpret_cast<LPWSTR>(pNextString);
			std::memcpy(pMonitorName, cPort.MonitorName.c_str(), cbMonitorName);

			size_t cbDescription = (cPort.Description.length() + 1) * sizeof(wchar_t);
			pNextString -= cbDescription; LPWSTR pDescription = reinterpret_cast<LPWSTR>(pNextString);
			std::memcpy(pDescription, cPort.Description.c_str(), cbDescription);

			pNextStruct->pPortName = pPortName;
			pNextStruct->pMonitorName = pMonitorName;
			pNextStruct->pDescription = pMonitorName;
			pNextStruct->fPortType = PORT_TYPE_WRITE;
			pNextStruct->Reserved = 0;

			pNextStruct += 1;
		}
	}

	*pcbNeeded = (DWORD)cbReq;
	*pcReturned = (DWORD)portInfos.size();
	return TRUE;
}

_Success_(return != FALSE)
BOOL WINAPI	LcmOpenPort(
	_In_ HANDLE  hMonitor,
	_In_ LPWSTR  pName,
	_Out_ PHANDLE pHandle)
{
	if (!hMonitor || !pName)
	{
		SetLastError(ERROR_INVALID_PARAMETER);
		*pHandle = 0;
		return FALSE;
	}

	auto pEsVpPortMon = reinterpret_cast<EsVpPortMon*>(hMonitor);
	auto pPort = pEsVpPortMon->GetPort(std::wstring(pName));
	if (pPort == nullptr)
	{
		SetLastError(ERROR_FILE_NOT_FOUND);
		*pHandle = 0;
		return FALSE;
	}

	*pHandle = reinterpret_cast<HANDLE>(pPort);
	return TRUE;
}

BOOL WINAPI LcmStartDocPort(
	_In_             HANDLE  hPort,
	_In_             LPWSTR  pPrinterName,
	DWORD   JobId,
	_In_range_(1, 2) DWORD   level,
	_When_(level == 1, _In_reads_bytes_(sizeof(DOC_INFO_1)))
	_When_(level == 2, _In_reads_bytes_(sizeof(DOC_INFO_2)))
	LPBYTE  pDocInfo)
{
	UNREFERENCED_PARAMETER(level);

	if (!hPort || !pPrinterName || !pDocInfo)
	{
		SetLastError(ERROR_INVALID_PARAMETER);
		return FALSE;
	}

	auto pPort = reinterpret_cast<EsVpPort*>(hPort);
	auto docInfo = reinterpret_cast<DOC_INFO_1*>(pDocInfo);
	auto hr = pPort->TryStartDoc(std::wstring(pPrinterName), (int)JobId, docInfo);
	SetLastError(hr);
	return SUCCEEDED(hr);
}

_Success_(return != FALSE)
BOOL WINAPI LcmWritePort(
	_In_ HANDLE hPort,
	_In_reads_bytes_(cbBuf) LPBYTE  pBuffer,
	DWORD cbBuf,
	_Out_ LPDWORD pcbWritten)
{
	if (!hPort || !pBuffer || !pcbWritten)
	{
		SetLastError(ERROR_INVALID_PARAMETER);
		return FALSE;
	}

	auto pPort = reinterpret_cast<EsVpPort*>(hPort);
	auto hr = pPort->WritePort(pBuffer, cbBuf, pcbWritten);
	SetLastError(hr);
	return SUCCEEDED(hr);
}

_Success_(return != FALSE)
BOOL WINAPI LcmReadPort(
	_In_ HANDLE hPort,
	_Out_writes_bytes_(cbBuf) LPBYTE pBuffer,
	DWORD cbBuf,
	_Out_ LPDWORD pcbRead)
{
	UNREFERENCED_PARAMETER(hPort);
	UNREFERENCED_PARAMETER(pBuffer);
	UNREFERENCED_PARAMETER(cbBuf);

	*pcbRead = 0;
	return FALSE;
}

BOOL WINAPI LcmEndDocPort(_In_ HANDLE hPort)
{
	UNREFERENCED_PARAMETER(hPort);
	return TRUE;
}

BOOL WINAPI LcmClosePort(_In_ HANDLE hPort)
{
	if (!hPort)
	{
		SetLastError(ERROR_INVALID_PARAMETER);
		return FALSE;
	}

	auto pPort = reinterpret_cast<EsVpPort*>(hPort);
	delete pPort;
	return TRUE;
}

BOOL WINAPI LcmSetPortTimeOuts(
	_In_ HANDLE hPort,
	_In_ LPCOMMTIMEOUTS lpCTO,
	_Reserved_ DWORD reserved)
{
	UNREFERENCED_PARAMETER(hPort);
	UNREFERENCED_PARAMETER(lpCTO);
	UNREFERENCED_PARAMETER(reserved);

	SetLastError(ERROR_INVALID_PARAMETER);
	return FALSE;
}

_Success_(return == NO_ERROR)
DWORD WINAPI LcmXcvDataPort(
	_In_ HANDLE  hXcv,
	_In_ LPCWSTR pszDataName,
	_In_reads_bytes_(cbInputData) PBYTE pInputData,
	_In_ DWORD cbInputData,
	_Out_writes_bytes_(cbOutputData) PBYTE pOutputData,
	_In_ DWORD cbOutputData,
	_Out_ PDWORD pcbOutputNeeded)
{
	UNREFERENCED_PARAMETER(hXcv);
	UNREFERENCED_PARAMETER(pszDataName);
	UNREFERENCED_PARAMETER(pInputData);
	UNREFERENCED_PARAMETER(cbInputData);
	UNREFERENCED_PARAMETER(pOutputData);
	UNREFERENCED_PARAMETER(cbOutputData);
	UNREFERENCED_PARAMETER(pcbOutputNeeded);

	return ERROR_INVALID_PARAMETER;
}

_Success_(return == TRUE)
BOOL WINAPI LcmXcvOpenPort(
	_In_ HANDLE hMonitor,
	LPCWSTR pszObject,
	ACCESS_MASK GrantedAccess,
	PHANDLE phXcv)
{
	UNREFERENCED_PARAMETER(hMonitor);
	UNREFERENCED_PARAMETER(pszObject);
	UNREFERENCED_PARAMETER(GrantedAccess);
	UNREFERENCED_PARAMETER(phXcv);

	SetLastError(ERROR_ACCESS_DENIED);
	return FALSE;
}

_Success_(return == TRUE)
BOOL WINAPI LcmXcvClosePort(_In_ HANDLE hXcv)
{
	UNREFERENCED_PARAMETER(hXcv);

	return TRUE;
}

VOID WINAPI LcmShutdown(_In_ HANDLE hMonitor)
{
	if (hMonitor)
	{
		auto pEsVpPortMon = reinterpret_cast<EsVpPortMon*>(hMonitor);
		delete pEsVpPortMon;
	}
}

void InitializeDispatchTable(PMONITOR2 dt)
{
	*dt = {
		sizeof(MONITOR2),
		LcmEnumPorts,
		LcmOpenPort,
		NULL,           // OpenPortEx is not supported
		LcmStartDocPort,
		LcmWritePort,
		LcmReadPort,
		LcmEndDocPort,
		LcmClosePort,
		NULL,           // AddPort is not supported
		NULL,           // LcmAddPortEx,
		NULL,           // ConfigurePort is not supported
		NULL,           // DeletePort is not supported
		NULL,			// LcmGetPrinterDataFromPort,
		LcmSetPortTimeOuts,
		LcmXcvOpenPort,
		LcmXcvDataPort,
		LcmXcvClosePort,
		LcmShutdown
	};
}