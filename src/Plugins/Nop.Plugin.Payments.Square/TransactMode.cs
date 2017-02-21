namespace Nop.Plugin.Payments.Square
{
    /// <summary>
    /// Represents payment processor transaction mode
    /// </summary>
    public enum TransactMode
    {
        /// <summary>
        /// Authorize
        /// </summary>
        Authorize = 0,

        /// <summary>
        /// Authorize and capture
        /// </summary>
        AuthorizeAndCapture = 2
    }
}
