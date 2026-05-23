namespace NexusLabs.Framework.IO;

public readonly record struct SubstreamOptions(
    long Offset,
    long Length,
    bool TakeOwnershipOfStream = false,
    bool AssumeParentStreamOffsetCorrect = true);
