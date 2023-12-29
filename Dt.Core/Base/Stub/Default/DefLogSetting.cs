#region 文件描述
/******************************************************************************
* 创建: Daoting
* 摘要: 
* 日志: 2022-02-28 创建
******************************************************************************/
#endregion

#region 引用命名
using Serilog.Events;
#endregion

namespace Dt.Core
{
    /// <summary>
    /// 默认日志设置
    /// </summary>
    class DefLogSetting : ILogSetting
    {
        public bool ConsoleEnabled => true;

        public LogEventLevel ConsoleLogLevel => LogEventLevel.Debug;

        public bool FileEnabled => true;

        public LogEventLevel FileLogLevel => LogEventLevel.Information;

        public bool TraceEnabled => true;

        public LogEventLevel TraceLogLevel => LogEventLevel.Debug;
    }
}