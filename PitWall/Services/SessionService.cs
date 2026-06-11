using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace PitWall.Services;

public class SessionService
{
    private readonly F1APIService _apiService;
    private readonly CacheService _cacheService;

    private readonly Dictionary<int, List<F1Session>> _sessionsByYear = new();

    public SessionService(F1APIService apiService, CacheService cacheService)
    {
        _apiService = apiService;
        _cacheService = cacheService;
    }

    public async Task StartAsync()
    {
        await LoadYearAsync(2023);
        await LoadYearAsync(2024);
        await LoadYearAsync(2025);
        await LoadYearAsync(2026);
    }

    public async Task<IReadOnlyList<F1Session>> LoadYearAsync(int year)
    {

    }
    
}