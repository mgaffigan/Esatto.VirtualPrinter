#pragma once

#include <windows.h>
#include <userenv.h>
#pragma comment(lib, "userenv.lib")
#include <winspool.h>
#include <winsplp.h>

#include <memory>
#include <string>
#include <vector>
#include <bit>

#include <comutil.h>
#pragma comment(lib, "comsuppw.lib")
#include <wil/com.h>
#include <wil/result.h>
#include <wil/resource.h>

#include "DriverConstants.h"

#include "Entrypoint.h"
#include "PrintJob.h"
#include "EsVpPort.h"
#include "EsVpPortMon.h"

#include "OleAuto.h"
#include <sddl.h>


