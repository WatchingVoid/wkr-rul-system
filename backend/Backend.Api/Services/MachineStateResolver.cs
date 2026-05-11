using Backend.Api.Models;

namespace Backend.Api.Services;

public sealed class MachineStateResolver
{
    public MachineStateInfo Resolve(TelemetryFrame frame)
    {
        // 1. Если collector явно передал состояние остановки — доверяем ему.
        if (IsStopFrame(frame))
        {
            return new MachineStateInfo
            {
                MachineState = frame.MachineState ?? "stopped",
                SpindleState = frame.SpindleState ?? "stopped",
                StopRequired = frame.StopRequired ?? true,
                StopReason = frame.StopReason ?? "TOOL_RUL_CRITICAL",
                ControlAction = frame.ControlAction ?? "SPINDLE_STOP",

                EventCode = "MACHINE_STOP_RUL_CRITICAL",
                EventLevel = 2,
                EventMessage = "Станок остановлен из-за критического остаточного ресурса инструмента."
            };
        }

        // 2. Резание идёт.
        if (frame.CutFlag && frame.SpindleRpm > 0)
        {
            return new MachineStateInfo
            {
                MachineState = frame.MachineState ?? "cutting",
                SpindleState = frame.SpindleState ?? "rotating",
                StopRequired = frame.StopRequired ?? false,
                StopReason = frame.StopReason,
                ControlAction = frame.ControlAction,

                EventCode = "MACHINE_CUTTING",
                EventLevel = 0,
                EventMessage = "Станок выполняет обработку."
            };
        }

        // 3. Шпиндель вращается, но резание не идёт.
        if (!frame.CutFlag && frame.SpindleRpm > 0)
        {
            return new MachineStateInfo
            {
                MachineState = frame.MachineState ?? "running",
                SpindleState = frame.SpindleState ?? "rotating",
                StopRequired = frame.StopRequired ?? false,
                StopReason = frame.StopReason,
                ControlAction = frame.ControlAction,

                EventCode = "MACHINE_RUNNING_NO_CUT",
                EventLevel = 0,
                EventMessage = "Шпиндель вращается, активное резание не зафиксировано."
            };
        }

        // 4. Станок не режет, шпиндель остановлен.
        if (!frame.CutFlag && frame.SpindleRpm <= 0)
        {
            return new MachineStateInfo
            {
                MachineState = frame.MachineState ?? "idle",
                SpindleState = frame.SpindleState ?? "stopped",
                StopRequired = frame.StopRequired ?? false,
                StopReason = frame.StopReason,
                ControlAction = frame.ControlAction,

                EventCode = "MACHINE_IDLE",
                EventLevel = 0,
                EventMessage = "Станок находится в состоянии ожидания, шпиндель остановлен."
            };
        }

        return new MachineStateInfo
        {
            MachineState = frame.MachineState ?? "unknown",
            SpindleState = frame.SpindleState ?? "unknown",
            StopRequired = frame.StopRequired ?? false,
            StopReason = frame.StopReason,
            ControlAction = frame.ControlAction,

            EventCode = "MACHINE_STATE_UNKNOWN",
            EventLevel = 0,
            EventMessage = "Состояние станка не определено."
        };
    }

    private static bool IsStopFrame(TelemetryFrame frame)
    {
        if (string.Equals(frame.Program, "MACHINE_STOP_RUL_CRITICAL", StringComparison.OrdinalIgnoreCase))
            return true;

        if (string.Equals(frame.MachineState, "stopped", StringComparison.OrdinalIgnoreCase)
            && string.Equals(frame.StopReason, "TOOL_RUL_CRITICAL", StringComparison.OrdinalIgnoreCase))
            return true;

        if (frame.StopRequired == true
            && frame.SpindleRpm <= 0
            && frame.SpindlePowerKw <= 0
            && frame.SpindleCurrentA <= 0)
            return true;

        return false;
    }
}