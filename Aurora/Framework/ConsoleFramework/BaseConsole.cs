using System;
using System.Reflection;
using log4net;
using log4net.Core;

namespace Aurora.Framework
{
    public class BaseConsole
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public Level MaxLogLevel { get; set; }

        #region ILog Members

        public bool IsDebugEnabled
        {
            get { return m_log.IsDebugEnabled; }
        }

        public bool IsErrorEnabled
        {
            get { return m_log.IsErrorEnabled; }
        }

        public bool IsFatalEnabled
        {
            get { return m_log.IsFatalEnabled; }
        }

        public bool IsInfoEnabled
        {
            get { return m_log.IsInfoEnabled; }
        }

        public bool IsWarnEnabled
        {
            get { return m_log.IsInfoEnabled; }
        }

        public void Debug(object message)
        {
            m_log.Debug(message);
        }

        public void Debug(object message, Exception exception)
        {
            m_log.Debug(message, exception);
        }

        public void DebugFormat(string format, object arg0)
        {
            m_log.DebugFormat(format, arg0);
        }

        public void DebugFormat(string format, params object[] args)
        {
            m_log.DebugFormat(format, args);
        }

        public void DebugFormat(IFormatProvider provider, string format, params object[] args)
        {
            m_log.DebugFormat(provider, format, args);
        }

        public void DebugFormat(string format, object arg0, object arg1)
        {
            m_log.DebugFormat(format, arg0, arg1);
        }

        public void DebugFormat(string format, object arg0, object arg1, object arg2)
        {
            m_log.DebugFormat(format, arg0, arg1, arg2);
        }

        public void Error(object message)
        {
            m_log.Error(message);
        }

        public void Error(object message, Exception exception)
        {
            m_log.Error(message, exception);
        }

        public void ErrorFormat(string format, object arg0)
        {
            m_log.ErrorFormat(format, arg0);
        }

        public void ErrorFormat(string format, params object[] args)
        {
            m_log.ErrorFormat(format, args);
        }

        public void ErrorFormat(IFormatProvider provider, string format, params object[] args)
        {
            m_log.ErrorFormat(format, provider, args);
        }

        public void ErrorFormat(string format, object arg0, object arg1)
        {
            m_log.ErrorFormat(format, arg0, arg1);
        }

        public void ErrorFormat(string format, object arg0, object arg1, object arg2)
        {
            m_log.ErrorFormat(format, arg0, arg1, arg2);
        }

        public void Fatal(object message)
        {
            m_log.Fatal(message);
        }

        public void Fatal(object message, Exception exception)
        {
            m_log.Fatal(message, exception);
        }

        public void FatalFormat(string format, object arg0)
        {
            m_log.FatalFormat(format, arg0);
        }

        public void FatalFormat(string format, params object[] args)
        {
            m_log.FatalFormat(format, args);
        }

        public void FatalFormat(IFormatProvider provider, string format, params object[] args)
        {
            m_log.FatalFormat(provider, format, args);
        }

        public void FatalFormat(string format, object arg0, object arg1)
        {
            m_log.FatalFormat(format, arg0, arg1);
        }

        public void FatalFormat(string format, object arg0, object arg1, object arg2)
        {
            m_log.FatalFormat(format, arg0, arg1, arg2);
        }

        public void Format(Level level, string format, object arg0)
        {
            m_log.Format(level, format, arg0);
        }

        public void Format(Level level, string format, params object[] args)
        {
            m_log.Format(level, format, args);
        }

        public void Format(Level level, IFormatProvider provider, string format, params object[] args)
        {
            m_log.Format(level, provider, format, args);
        }

        public void Format(Level level, string format, object arg0, object arg1)
        {
            m_log.Format(level, format, arg0, arg1);
        }

        public void Format(Level level, string format, object arg0, object arg1, object arg2)
        {
            m_log.Format(level, format, arg0, arg1, arg2);
        }

        public void Info(object message)
        {
            m_log.Info(message);
        }

        public void Info(object message, Exception exception)
        {
            m_log.Info(message, exception);
        }

        public void InfoFormat(string format, object arg0)
        {
            m_log.InfoFormat(format, arg0);
        }

        public void InfoFormat(string format, params object[] args)
        {
            m_log.InfoFormat(format, args);
        }

        public void InfoFormat(IFormatProvider provider, string format, params object[] args)
        {
            m_log.InfoFormat(provider, format, args);
        }

        public void InfoFormat(string format, object arg0, object arg1)
        {
            m_log.InfoFormat(format, arg0, arg1);
        }

        public void InfoFormat(string format, object arg0, object arg1, object arg2)
        {
            m_log.InfoFormat(format, arg0, arg1, arg2);
        }

        public bool IsEnabled(Level level)
        {
            return m_log.IsEnabled(level);
        }

        public void Log(Level level, object message)
        {
            m_log.Log(level, message);
        }

        public void Log(Level level, object message, Exception exception)
        {
            m_log.Log(level, message, exception);
        }

        public void Trace(object message)
        {
            m_log.Trace(message);
        }

        public void Trace(object message, Exception exception)
        {
            m_log.Trace(message, exception);
        }

        public void TraceFormat(string format, object arg0)
        {
            m_log.TraceFormat(format, arg0);
        }

        public void TraceFormat(string format, params object[] args)
        {
            m_log.TraceFormat(format, args);
        }

        public void TraceFormat(IFormatProvider provider, string format, params object[] args)
        {
            m_log.TraceFormat(provider, format, args);
        }

        public void TraceFormat(string format, object arg0, object arg1)
        {
            m_log.TraceFormat(format, arg0, arg1);
        }

        public void TraceFormat(string format, object arg0, object arg1, object arg2)
        {
            m_log.TraceFormat(format, arg0, arg1, arg2);
        }

        public void Warn(object message)
        {
            m_log.Warn(message);
        }

        public void Warn(object message, Exception exception)
        {
            m_log.Warn(message, exception);
        }

        public void WarnFormat(string format, object arg0)
        {
            m_log.WarnFormat(format, arg0);
        }

        public void WarnFormat(string format, params object[] args)
        {
            m_log.WarnFormat(format, args);
        }

        public void WarnFormat(IFormatProvider provider, string format, params object[] args)
        {
            m_log.WarnFormat(provider, format, args);
        }

        public void WarnFormat(string format, object arg0, object arg1)
        {
            m_log.WarnFormat(format, arg0, arg1);
        }

        public void WarnFormat(string format, object arg0, object arg1, object arg2)
        {
            m_log.WarnFormat(format, arg0, arg1, arg2);
        }

        #endregion
    }
}
