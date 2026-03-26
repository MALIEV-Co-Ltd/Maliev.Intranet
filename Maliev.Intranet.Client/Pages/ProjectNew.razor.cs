using Maliev.Intranet.Client.Components.Project;
using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using System.Net.Http.Json;
using MudBlazor;

namespace Maliev.Intranet.Client.Pages;

/// <summary>New project creation page — shell with stub state. Logic added in Tasks 4–17.</summary>
public partial class ProjectNew : IAsyncDisposable
{
    [Inject] private IDialogService DialogService { get; set; } = null!;

    // ── Project-level state ───────────────────────────────────────────
    private Guid _tempProjectId = Guid.NewGuid();  // non-readonly; reassigned on Duplicate
    private string _title = string.Empty;
    private string? _description;
    private CustomerSummaryDto? _selectedCustomer;
    private bool _showCustomerSearch = true;
    private CurrencyDto? _selectedCurrency;
    private List<CurrencyDto> _currencies = [];
    private List<LeadTimeOptionDto> _leadTimeOptions = [];
    private LeadTimeOptionDto? _selectedLeadTime;
    private List<ProcessDto> _processes = [];
#pragma warning disable CS0649
    private bool _saving;
    private bool _autoSaving;
    private DateTimeOffset? _lastSavedAt;
#pragma warning restore CS0649
    private LayoutMode _layoutMode = LayoutMode.Configurator;
    private bool _dragOver;
    private MudFileUpload<IReadOnlyList<IBrowserFile>>? _fileUpload;

    // ── Validation ────────────────────────────────────────────────────
    private string? _titleError;
    private bool _titleHasError;
    private string? _descriptionError;
    private bool _descriptionHasError;

    // ── Parts ─────────────────────────────────────────────────────────
    private readonly List<PartViewModel> _parts = [];
    private int _selectedPartIndex;

    // ── Upload / polling (preserved from original) ────────────────────
    private readonly Dictionary<string, CancellationTokenSource> _pollingTokens = new();
    private const int PollingIntervalMs = 2000;

    // ── Pricing debounce ──────────────────────────────────────────────
    private readonly Dictionary<Guid, CancellationTokenSource> _pricingTokens = new();
    private const int PricingDebounceMs = 300;

    // ── Auto-save debounce ────────────────────────────────────────────
#pragma warning disable CS0649
    private CancellationTokenSource? _autoSaveCts;
#pragma warning restore CS0649
    private const int AutoSaveDebounceMs = 1000;
    private const string DraftStorageKey = "project-new-draft";

    // ── File type sets (preserved from original) ──────────────────────
    private static readonly HashSet<string> ThreeDExtensions =
        new(StringComparer.OrdinalIgnoreCase)
        { ".stl", ".step", ".stp", ".3mf", ".obj", ".igs", ".iges", ".blend", ".fbx", ".gltf", ".glb" };

    private static readonly HashSet<string> AllowedExtensions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ".stl", ".step", ".stp", ".3mf", ".obj", ".igs", ".iges", ".blend", ".fbx", ".gltf", ".glb",
            ".pdf", ".dxf", ".dwg",
            ".png", ".jpg", ".jpeg", ".tiff", ".bmp", ".webp",
            ".doc", ".docx", ".xls", ".xlsx",
            ".zip", ".rar", ".7z",
        };

    private bool CanSubmit =>
        !_saving &&
        _selectedCustomer != null &&
        !string.IsNullOrWhiteSpace(_title) &&
        !_titleHasError &&
        !_descriptionHasError &&
        _selectedLeadTime != null &&
        !_parts.Any(p => p.Uploading || p.PricingLoading);

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        foreach (var cts in _pollingTokens.Values) { await cts.CancelAsync(); cts.Dispose(); }
        foreach (var cts in _pricingTokens.Values) { await cts.CancelAsync(); cts.Dispose(); }
        _autoSaveCts?.Cancel();
        _autoSaveCts?.Dispose();
    }

    // ── Stubs — implemented in later tasks ────────────────────────────
    /// <inheritdoc />
    protected override async Task OnInitializedAsync() { await Task.CompletedTask; }

    private Task OpenFilePicker() => _fileUpload?.OpenFilePickerAsync() ?? Task.CompletedTask;

    private static bool Is3DFile(string name) =>
        ThreeDExtensions.Contains(Path.GetExtension(name));

    private static string GetFileIcon(string name) =>
        Path.GetExtension(name).ToLowerInvariant() switch
        {
            ".stl" or ".step" or ".stp" or ".3mf" or ".obj" or ".igs" or ".iges"
            or ".blend" or ".fbx" or ".gltf" or ".glb" => Icons.Material.Outlined.ViewInAr,
            ".pdf" => Icons.Material.Outlined.PictureAsPdf,
            ".dxf" or ".dwg" => Icons.Material.Outlined.Architecture,
            ".png" or ".jpg" or ".jpeg" or ".tiff" or ".bmp" or ".webp" => Icons.Material.Outlined.Image,
            ".doc" or ".docx" => Icons.Material.Outlined.Description,
            ".xls" or ".xlsx" => Icons.Material.Outlined.TableChart,
            ".zip" or ".rar" or ".7z" => Icons.Material.Outlined.FolderZip,
            _ => Icons.Material.Outlined.InsertDriveFile,
        };

    private string? ResolvePreviewUrl(string? url)
    {
        if (string.IsNullOrEmpty(url)) return null;
        if (url.StartsWith("http://") || url.StartsWith("https://")) return url;
        var baseUri = Navigation.BaseUri.TrimEnd('/');
        return url.StartsWith("/") ? $"{baseUri}{url}" : $"{baseUri}/{url}";
    }

    private void ValidateTitle()
    {
        if (string.IsNullOrWhiteSpace(_title)) { _titleError = "Project title is required"; _titleHasError = true; }
        else if (_title.Length > 500) { _titleError = "Must be 500 characters or fewer"; _titleHasError = true; }
        else { _titleError = null; _titleHasError = false; }
    }

    private void ValidateDescription()
    {
        if (_description?.Length > 2000) { _descriptionError = "Must be 2000 characters or fewer"; _descriptionHasError = true; }
        else { _descriptionError = null; _descriptionHasError = false; }
    }

    private Task HandleFileSelected(IReadOnlyList<IBrowserFile> files) => Task.CompletedTask; // Task 10
    private void RemovePart(PartViewModel part) { } // Task 10
    private void OnPartChanged(PartViewModel part) { } // Task 12
    private void TriggerAutoSave() { } // Task 14
    private void DuplicateProject() { } // Task 15
    private Task OpenBabylonViewer(PartViewModel part) => Task.CompletedTask; // Task 16
    private Task CreateProjectAndQuoteAsync() => Task.CompletedTask; // Task 15
    private Task<IEnumerable<CustomerSummaryDto>> SearchCustomersAsync(string value, CancellationToken ct)
        => Task.FromResult(Enumerable.Empty<CustomerSummaryDto>()); // Task 4
    private void OnCustomerSelected(CustomerSummaryDto? customer)
    { _selectedCustomer = customer; if (customer != null) _showCustomerSearch = false; }
}
