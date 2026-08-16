using System;

namespace Application.Utils.Helper.SupabaseHelper;

/// <summary>
/// Minimal listing metadata for a raw Supabase Storage object, decoupled from
/// the 3rd-party Supabase.Storage.FileObject type so fakes don't need
/// to construct it. Name is relative to the queried prefix —
/// the same shape the Supabase Storage List API returns.
/// </summary>
public sealed record SupabaseStorageEntry(string Name, DateTimeOffset? UpdatedAt);
