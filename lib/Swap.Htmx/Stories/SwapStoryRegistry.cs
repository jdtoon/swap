using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace Swap.Htmx.Stories;

/// <summary>
/// Registry responsible for discovering and organizing Swap stories from controller actions.
/// </summary>
internal class SwapStoryRegistry
{
    private readonly ApplicationPartManager _partManager;
    private readonly ConcurrentDictionary<string, List<StoryDefinition>> _groupedStories = new();
    private bool _scanned = false;
    private readonly object _lock = new();

    public SwapStoryRegistry(ApplicationPartManager partManager)
    {
        _partManager = partManager;
    }

    public IReadOnlyDictionary<string, List<StoryDefinition>> GetStories()
    {
        if (!_scanned)
        {
            lock (_lock)
            {
                if (!_scanned)
                {
                    ScanStories();
                    _scanned = true;
                }
            }
        }
        return _groupedStories;
    }

    private void ScanStories()
    {
        var feature = new ControllerFeature();
        _partManager.PopulateFeature(feature);

        foreach (var controller in feature.Controllers)
        {
            if (!typeof(SwapController).IsAssignableFrom(controller.AsType()))
                continue;

            var methods = controller.AsType().GetMethods(BindingFlags.Public | BindingFlags.Instance);
            foreach (var method in methods)
            {
                var storyAttr = method.GetCustomAttribute<SwapStoryAttribute>();
                if (storyAttr == null) continue;

                var controllerName = controller.Name.EndsWith("Controller") 
                    ? controller.Name.Substring(0, controller.Name.Length - 10) 
                    : controller.Name;

                var story = new StoryDefinition
                {
                    Id = Guid.NewGuid().ToString("N"),
                    Title = storyAttr.Title,
                    Category = storyAttr.Category,
                    Description = storyAttr.Description,
                    Width = storyAttr.Width,
                    Height = storyAttr.Height,
                    ControllerName = controllerName,
                    ActionName = method.Name,
                    RouteUrl = $"/{controllerName}/{method.Name}" // Simplified, assumes default route convention
                };

                if (!_groupedStories.TryGetValue(story.Category, out var list))
                {
                    list = new List<StoryDefinition>();
                    _groupedStories[story.Category] = list;
                }
                list.Add(story);
            }
        }
    }
}

internal class StoryDefinition
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string Category { get; set; } = "";
    public string? Description { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public string ControllerName { get; set; } = "";
    public string ActionName { get; set; } = "";
    public string RouteUrl { get; set; } = "";
}
