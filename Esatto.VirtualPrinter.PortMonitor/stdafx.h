#pragma once

#include <windows.h>
#include <winspool.h>
#include <winsplp.h>

#include <string>
#include <vector>

#include "DriverConstants.h"

#include "Entrypoint.h"
#include "EsVpPort.h"
#include "EsVpPortMon.h"

#include "OleAuto.h"
#include <sddl.h>

#if DEBUG
#import "..\Esatto.VirtualPrinter.Common\bin\Debug\Esatto.VirtualPrinter.Common.tlb"
#else
#import "..\Esatto.VirtualPrinter.Common\bin\Release\Esatto.VirtualPrinter.Common.tlb"
#endif
