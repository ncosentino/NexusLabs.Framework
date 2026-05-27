using System;
using System.Collections.Generic;

namespace NexusLabs.Framework;

/// <summary>
/// Declares that the annotated member transfers ownership of a disposable
/// resource to the declaring type. Used by NLF's
/// <c>TransfersOwnershipDisposeSuppressor</c> (NLFSUP001) to suppress
/// IDISP007 ("Don't dispose injected") in the two canonical shapes.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Shape B (direct, parameterless):</strong> apply to the disposable
/// field/property whose <c>Dispose</c> call should be considered "owned".
/// No <see cref="Targets"/> are needed — the attribute placement IS the
/// link between the ownership claim and the disposable.
/// </para>
/// <code>
/// [TransfersOwnership]
/// private readonly Stream _inner;
///
/// public void Dispose() =&gt; _inner.Dispose(); // IDISP007 suppressed
/// </code>
/// <para>
/// <strong>Shape A (conditional, strict targets):</strong> apply to a
/// boolean field, property, or parameter that gates conditional ownership.
/// You MUST list the simple names of the disposable members the flag
/// authorises (one or more). Use <c>nameof(...)</c> to keep the link
/// rename-safe:
/// </para>
/// <code>
/// [TransfersOwnership(nameof(_inner))]
/// private readonly bool _takeOwnership;
///
/// protected override void Dispose(bool disposing)
/// {
///     if (disposing &amp;&amp; _takeOwnership)
///     {
///         _inner.Dispose();        // IDISP007 suppressed (in targets)
///         _otherInjected.Dispose();// IDISP007 STILL fires (not in targets)
///     }
/// }
/// </code>
/// <para>
/// A flag annotated with no targets does NOT suppress any IDISP007.
/// This is a deliberate constraint — silencing every dispose inside a
/// guard regardless of which field it disposes is the bug class strict
/// targeting prevents.
/// </para>
/// <para>
/// May also be applied to a constructor parameter to document the
/// transfer intent at the call site; the suppressor does not currently
/// trace assignment dataflow from parameters to fields, so apply Shape B
/// to the backing field as well when you want suppression of a direct
/// (non-conditional) dispose.
/// </para>
/// </remarks>
[AttributeUsage(
    AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Parameter,
    AllowMultiple = false,
    Inherited = false)]
public sealed class TransfersOwnershipAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of <see cref="TransfersOwnershipAttribute"/>.
    /// </summary>
    /// <param name="targets">
    /// Simple names of the disposable fields/properties whose disposal is
    /// authorised by this annotation. Required for Shape A (annotation on
    /// a flag/parameter). Ignored for Shape B (annotation on the disposable
    /// itself). Use <c>nameof(...)</c> to keep the names rename-safe.
    /// </param>
    public TransfersOwnershipAttribute(params string[] targets)
    {
        Targets = targets ?? Array.Empty<string>();
    }

    /// <summary>
    /// Simple names of the disposable members this annotation authorises
    /// to be disposed inside guards that read the annotated flag. Empty
    /// for Shape B (placement on the disposable itself).
    /// </summary>
    public IReadOnlyList<string> Targets { get; }
}
