using NexusLabs.StronglyTypedIds;

using StronglyTypedIds;

namespace NexusLabs.StronglyTypedIds.Tests;

/// <summary>
/// Test identifier generated from the built-in GUID template and the additive UUIDv7 template.
/// </summary>
[StronglyTypedId(Template.Guid, GuidIdTemplates.UuidV7)]
internal readonly partial struct TestUuidV7Id;
