using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace JBlam.HarClient
{
    /// <summary>
    /// Defines a policy for handling redirect messages between a <see cref="DelegatingHandler"/>
    /// and its inner handler.
    /// </summary>
    public readonly struct RedirectPolicy : IEquatable<RedirectPolicy>
    {
        /// <summary>
        /// The outer handler will attempt to suppress any automatic redirect behaviour in the
        /// inner handler, but will follow the redirects itself automatically.
        /// </summary>
        public static readonly RedirectPolicy SuppressAndFollow = new RedirectPolicy(true, true);
        /// <summary>
        /// The outer handler will attempt to suppress any automatic redirect behaviour in the
        /// inner handler, and will return any redirect messages directly instead of following
        /// them.
        /// </summary>
        public static readonly RedirectPolicy SuppressOnly = new RedirectPolicy(false, true);
        /// <summary>
        /// The outer handler will allow the inner handler to redirect automatically if it chooses;
        /// it will return any redirect messages if they are observed.
        /// </summary>
        public static readonly RedirectPolicy DoNotSuppress = new RedirectPolicy(false, false);

        public RedirectPolicy(bool followRedirects, bool suppressInnerAutoRedirect)
        {
            FollowRedirects = followRedirects;
            SuppressInnerAutoRedirect = suppressInnerAutoRedirect;
        }
        /// <summary>
        /// Gets the observable auto redirect policy. If true, the handler will follow redirects;
        /// otherwise, it will return any redirect response messages directly.
        /// </summary>
        public bool FollowRedirects { get; }
        /// <summary>
        /// Gets the inner-handler auto redirect policy. If true, the inner handler's auto redirect
        /// behaviour will be suppressed (where possible). Otherwise, the inner handler will behave
        /// as it was originally configured.
        /// </summary>
        public bool SuppressInnerAutoRedirect { get; }

        public override bool Equals(object? obj)
        {
            return obj is RedirectPolicy policy && Equals(policy);
        }

        public bool Equals(RedirectPolicy other)
        {
            return FollowRedirects == other.FollowRedirects &&
                   SuppressInnerAutoRedirect == other.SuppressInnerAutoRedirect;
        }

        public override int GetHashCode()
        {
            int hashCode = 1120676670;
            hashCode = hashCode * -1521134295 + FollowRedirects.GetHashCode();
            hashCode = hashCode * -1521134295 + SuppressInnerAutoRedirect.GetHashCode();
            return hashCode;
        }

        public static bool operator ==(RedirectPolicy left, RedirectPolicy right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(RedirectPolicy left, RedirectPolicy right)
        {
            return !(left == right);
        }
    }
}
