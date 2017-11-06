#include "stdafx.h"

EsVpPortMon::EsVpPortMon()
{
}

EsVpPortMon::~EsVpPortMon()
{
}

std::vector<PortRegistration> EsVpPortMon::EnumPorts()
{
	std::vector<PortRegistration> results;
	results.emplace_back(
		NMON_PORT_NAME,
		NMON_MONITOR_NAME,
		NMON_PORT_DESCRIPTION
	);
	return results;
}

EsVpPort* EsVpPortMon::GetPort(std::wstring portName)
{
	if (portName == NMON_PORT_NAME)
	{
		return new EsVpPort(NMON_PORT_NAME);
	}

	return nullptr;
}
