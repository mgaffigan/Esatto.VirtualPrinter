#pragma once

struct PortRegistration {
	std::wstring PortName;
	std::wstring MonitorName;
	std::wstring Description;

	PortRegistration(std::wstring portName, std::wstring monName, std::wstring desc)
	{
		this->PortName = portName;
		this->MonitorName = monName;
		this->Description = desc;
	}

	size_t GetRequiredLength()
	{
		return (PortName.length()
			+ MonitorName.length()
			+ Description.length()
			+ 3 /* null */) * sizeof(wchar_t);
	}
};