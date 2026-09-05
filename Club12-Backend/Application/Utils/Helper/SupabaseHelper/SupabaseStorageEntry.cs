using System;

namespace Application.Utils.Helper.SupabaseHelper;

/// <summary>
/// Minimal listing metadata for a raw Supabase Storage object, decoupled from the third-party Supabase.Storage.FileObject type so fakes don't need to construct it.
/// </summary>
public sealed record SupabaseStorageEntry(string Name, DateTimeOffset? UpdatedAt);
