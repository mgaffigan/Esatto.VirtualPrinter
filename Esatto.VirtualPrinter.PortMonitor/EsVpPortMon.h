#pragma once

class EsVpPortMon
{
public:
	EsVpPortMon();
	~EsVpPortMon();

	std::vector<PortRegistration> EnumPorts();
	EsVpPort* GetPort(std::wstring portName);
};

