﻿namespace Sitko.Blazor.CKEditor;

using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

[PublicAPI]
public abstract class BaseCKEditorComponent : InputText, IAsyncDisposable
{
    private DotNetObjectReference<BaseCKEditorComponent>? instance;

    private JsonSerializerOptions jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private bool rendered;
    protected string EditorValue { get; private set; } = "";
    [Inject] protected ICKEditorOptionsProvider OptionsProvider { get; set; } = null!;
    [Inject] protected IJSRuntime JsRuntime { get; set; } = null!;
    [Inject] protected ILogger<BaseCKEditorComponent> Logger { get; set; } = null!;
    [Parameter] public string Placeholder { get; set; } = "Enter text";
    [Parameter] public string Class { get; set; } = "";
    [Parameter] public string Style { get; set; } = "";

    [Parameter] public CKEditorConfig? Config { get; set; }

    protected ElementReference EditorRef { get; set; }
    public Guid Id { get; } = Guid.NewGuid();

    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        instance?.Dispose();
        return DestroyEditor();
    }

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        if (Value != EditorValue)
        {
            EditorValue = Value ?? "";
            if (rendered)
            {
                await UpdateEditorAsync();
            }
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        if (firstRender)
        {
            instance = DotNetObjectReference.Create(this);
            await InitializeEditorAsync(CancellationToken.None);
        }
    }

    private async Task InitializeEditorAsync(CancellationToken cancellationToken)
    {
        await JsRuntime.InvokeVoidAsync("window.SitkoBlazorCKEditor.init", cancellationToken, EditorRef,
            OptionsProvider.Options.EditorClassName, instance!, Id,
            JsonSerializer.Serialize(GetConfig(), jsonOptions));
        rendered = true;
    }

    private CKEditorConfig? GetConfig() => Config ?? OptionsProvider.Options.CKEditorConfig;


    protected ValueTask DestroyEditor()
    {
        rendered = false;
        return JsRuntime.InvokeVoidAsync("window.SitkoBlazorCKEditor.destroy", Id);
    }

    [JSInvokable]
    public Task UpdateText(string editorText)
    {
        if (EditorValue != editorText)
        {
            EditorValue = editorText;
            CurrentValue = EditorValue;
        }

        return Task.CompletedTask;
    }

    private async ValueTask UpdateEditorAsync() =>
        await JsRuntime.InvokeVoidAsync("window.SitkoBlazorCKEditor.update", Id, EditorValue);
}
