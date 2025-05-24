using System;

namespace RepGame.Network
{
    /// <summary>
    /// 网络响应常量，定义标准的响应状态码
    /// </summary>
    public static class NetworkResponseConstants
    {
        /// <summary>
        /// 成功状态码
        /// </summary>
        public const int SUCCESS_CODE = 200;

        /// <summary>
        /// 客户端请求错误状态码
        /// </summary>
        public const int CLIENT_ERROR_CODE = 400;

        /// <summary>
        /// 服务器错误状态码
        /// </summary>
        public const int SERVER_ERROR_CODE = 500;

        /// <summary>
        /// 检查响应是否成功
        /// </summary>
        /// <param name="code">响应状态码</param>
        /// <returns>是否成功</returns>
        public static bool IsSuccess(int code)
        {
            return code == SUCCESS_CODE;
        }
    }
}
