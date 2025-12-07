namespace SwapSmallPartials.Modules.Analytics.Models;

public class Product
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Category { get; set; }
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public int SalesToday { get; set; }
    public decimal RevenueToday { get; set; }
    public List<int> HourlySales { get; set; } = new();
}

public class Region
{
    public required string Name { get; set; }
    public int OrderCount { get; set; }
    public decimal Revenue { get; set; }
    public string TopProduct { get; set; } = "";
}

public class ActivityItem
{
    public DateTime Timestamp { get; set; }
    public required string CustomerName { get; set; }
    public required string ProductName { get; set; }
    public decimal Amount { get; set; }
}

public class AnalyticsState
{
    private readonly object _lock = new();
    
    // KPI Metrics
    public decimal RevenueToday { get; set; }
    public int OrdersToday { get; set; }
    public int ActiveUsersOnline { get; set; } = 247;
    public decimal ConversionRate { get; set; } = 3.2m;
    public decimal AvgOrderValue => OrdersToday > 0 ? RevenueToday / OrdersToday : 0;
    public decimal CartAbandonmentRate { get; set; } = 68.5m;
    
    // Products (12 top products)
    public List<Product> Products { get; set; } = new();
    
    // Categories (6 categories)
    public Dictionary<string, CategoryMetrics> Categories { get; set; } = new();
    
    // Regions (8 regions)
    public Dictionary<string, Region> Regions { get; set; } = new();
    
    // Hourly sales (24 hours)
    public decimal[] HourlySales { get; set; } = new decimal[24];
    public int CurrentHour { get; set; } = DateTime.Now.Hour;
    
    // Activity Feed (last 10 purchases)
    public Queue<ActivityItem> RecentActivity { get; set; } = new(10);
    
    // Customer Metrics
    public int NewCustomersToday { get; set; }
    public int ReturningCustomersToday { get; set; }
    public int VipCustomersActive { get; set; } = 12;
    public decimal CustomerSatisfactionScore { get; set; } = 4.6m;
    
    // Inventory Alerts
    public List<string> LowStockProducts => Products.Where(p => p.Stock < 10 && p.Stock > 0).Select(p => p.Name).ToList();
    public List<string> OutOfStockProducts => Products.Where(p => p.Stock == 0).Select(p => p.Name).ToList();
    
    public AnalyticsState()
    {
        InitializeData();
    }
    
    private void InitializeData()
    {
        // Initialize 12 products across 6 categories
        var categories = new[] { "Electronics", "Fashion", "Home", "Sports", "Books", "Beauty" };
        var productData = new[]
        {
            ("iPhone 15 Pro", "Electronics", 1299m, 45),
            ("MacBook Air M3", "Electronics", 1499m, 23),
            ("Nike Air Max", "Fashion", 149m, 87),
            ("Levi's Jeans", "Fashion", 89m, 156),
            ("Dyson Vacuum", "Home", 599m, 12),
            ("Ninja Blender", "Home", 129m, 34),
            ("Wilson Tennis Racket", "Sports", 179m, 28),
            ("Yoga Mat Premium", "Sports", 49m, 92),
            ("Atomic Habits", "Books", 27m, 203),
            ("Project Hail Mary", "Books", 18m, 145),
            ("Olay Regenerist", "Beauty", 39m, 167),
            ("Neutrogena Set", "Beauty", 25m, 198)
        };
        
        for (int i = 0; i < productData.Length; i++)
        {
            var (name, category, price, stock) = productData[i];
            Products.Add(new Product
            {
                Id = i + 1,
                Name = name,
                Category = category,
                Price = price,
                Stock = stock,
                SalesToday = 0,
                RevenueToday = 0,
                HourlySales = Enumerable.Repeat(0, 24).ToList()
            });
        }
        
        // Initialize categories
        foreach (var category in categories)
        {
            Categories[category] = new CategoryMetrics
            {
                Name = category,
                Revenue = 0,
                UnitsSold = 0,
                PercentOfTotal = 0
            };
        }
        
        // Initialize regions
        var regionNames = new[] { "Northeast", "Southeast", "Midwest", "Southwest", "West", "Northwest", "Central", "Gulf" };
        foreach (var region in regionNames)
        {
            Regions[region] = new Region
            {
                Name = region,
                OrderCount = 0,
                Revenue = 0,
                TopProduct = ""
            };
        }
    }
    
    public void ProcessPurchase(int productId, string region, string customerName, bool isNewCustomer, bool isVip)
    {
        lock (_lock)
        {
            var product = Products.FirstOrDefault(p => p.Id == productId);
            if (product == null || product.Stock == 0) return;
            
            // Update product
            product.Stock--;
            product.SalesToday++;
            product.RevenueToday += product.Price;
            product.HourlySales[CurrentHour]++;
            
            // Update KPIs
            RevenueToday += product.Price;
            OrdersToday++;
            
            // Random conversion rate fluctuation
            ConversionRate = Math.Round(3.0m + (decimal)(Random.Shared.NextDouble() * 0.8), 1);
            
            // Update category
            if (Categories.ContainsKey(product.Category))
            {
                Categories[product.Category].Revenue += product.Price;
                Categories[product.Category].UnitsSold++;
            }
            
            // Update region
            if (Regions.ContainsKey(region))
            {
                Regions[region].OrderCount++;
                Regions[region].Revenue += product.Price;
                Regions[region].TopProduct = product.Name;
            }
            
            // Update hourly sales
            HourlySales[CurrentHour] += product.Price;
            
            // Update customer metrics
            if (isNewCustomer) NewCustomersToday++;
            else ReturningCustomersToday++;
            
            if (isVip) VipCustomersActive = Math.Min(VipCustomersActive + 1, 50);
            
            // Add to activity feed
            var activity = new ActivityItem
            {
                Timestamp = DateTime.Now,
                CustomerName = customerName,
                ProductName = product.Name,
                Amount = product.Price
            };
            
            if (RecentActivity.Count >= 10)
                RecentActivity.Dequeue();
            RecentActivity.Enqueue(activity);
            
            // Recalculate category percentages
            RecalculateCategoryPercentages();
        }
    }
    
    public void ProcessCartAbandonment()
    {
        lock (_lock)
        {
            CartAbandonmentRate = Math.Round(65.0m + (decimal)(Random.Shared.NextDouble() * 8), 1);
        }
    }
    
    public void RestockAll()
    {
        lock (_lock)
        {
            foreach (var product in Products)
            {
                product.Stock = Random.Shared.Next(50, 200);
            }
        }
    }
    
    public void AdvanceHour()
    {
        lock (_lock)
        {
            CurrentHour = (CurrentHour + 1) % 24;
        }
    }
    
    private void RecalculateCategoryPercentages()
    {
        var total = Categories.Values.Sum(c => c.Revenue);
        if (total > 0)
        {
            foreach (var category in Categories.Values)
            {
                category.PercentOfTotal = Math.Round((category.Revenue / total) * 100, 1);
            }
        }
    }
}

public class CategoryMetrics
{
    public required string Name { get; set; }
    public decimal Revenue { get; set; }
    public int UnitsSold { get; set; }
    public decimal PercentOfTotal { get; set; }
}
