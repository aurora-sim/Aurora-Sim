
namespace Aurora.Framework
{
    public static class ProtocolVersion
    {
        /// <summary>
        ///     The current major protocol version of this version of Aurora
        /// </summary>
        public const int MAJOR_PROTOCOL_VERSION = 1;

        /// <summary>
        ///     The current minor protocol version of this version of Aurora
        /// </summary>
        public const int MINOR_PROTOCOL_VERSION = 0;

        /// <summary>
        ///     The minimum major protocol version allowed to connect to this version of Aurora
        /// </summary>
        public const int MINIMUM_MAJOR_PROTOCOL_VERSION = 1;

        /// <summary>
        ///     The minimum minor protocol version allowed to connect to this version of Aurora
        /// </summary>
        public const int MINIMUM_MINOR_PROTOCOL_VERSION = 0;
    }
}