namespace FWOffice.Components.Layout;

/// <summary>
/// A single node in the office navigation tree.
/// - A node with <see cref="Children"/> renders as a collapsible group.
/// - A node with <see cref="Enabled"/> = true and an <see cref="Href"/> renders as a live link.
/// - Anything else renders as a disabled "coming soon" placeholder.
/// </summary>
public record NavNode(
    string Label,
    string? Href = null,
    bool Enabled = false,
    List<NavNode>? Children = null,
    bool Expanded = false);
