using System;

namespace NexusLabs.Framework;

/// <summary>
/// Declares that the annotated member transfers ownership of a disposable
/// resource to the declaring type. Used by NLF's
/// <c>TransfersOwnershipDisposeSuppressor</c> (NLFSUP001) to suppress
/// IDISP007 ("Don't dispose injected") in the two canonical shapes.
/// </summary>
/// <remarks>
/// <para>
/// Apply to a disposable field/property whose <c>Dispose</c> call should
/// be considered "owned":
/// </para>
/// <code>
/// [TransfersOwnership]
/// private readonly Stream _inner;
///
/// public void Dispose() =&gt; _inner.Dispose(); // IDISP007 suppressed
/// </code>
/// <para>
/// Apply to a boolean field/property that gates conditional ownership:
/// </para>
/// <code>
/// [TransfersOwnership]
/// private readonly bool _takeOwnership;
///
/// protected override void Dispose(bool disposing)
/// {
///     if (disposing &amp;&amp; _takeOwnership)
///     {
///         _inner.Dispose(); // IDISP007 suppressed
///     }
/// }
/// </code>
/// <para>
/// May also be applied to a constructor parameter to document the
/// transfer intent at the call site; the suppressor does not currently
/// trace assignment dataflow from parameters to fields, so apply the
/// attribute to the backing field as well when you want suppression.
/// </para>
/// </remarks>
[AttributeUsage(
    AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Parameter,
    AllowMultiple = false,
    Inherited = false)]
public sealed class TransfersOwnershipAttribute : Attribute
{
}
