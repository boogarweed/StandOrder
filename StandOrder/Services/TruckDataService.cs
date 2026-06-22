// Services/TruckDataService.cs

using Microsoft.EntityFrameworkCore;
using StandOrder.Models; // Your models namespace

public class TruckDataService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public TruckDataService(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    // --- Lookup Methods ---
    public async Task<List<Supplier>> GetSuppliersAsync()
    {
        await using var context = _contextFactory.CreateDbContext();
        return await context.Suppliers.OrderBy(s => s.SupplierName).ToListAsync();
    }

    public async Task<List<Product>> GetProductsAsync()
    {
        await using var context = _contextFactory.CreateDbContext();
        var currentYear = DateTime.Now.Year.ToString();
        return await context.Products
            .Where(p => p.ProductYear == currentYear)
            .OrderBy(p => p.ItemNumber)
            .ToListAsync();
    }

    // --- Truck CRUD Operations ---
    public async Task<List<Truck>> GetTrucksByYearAsync(int year)
    {
        await using var context = _contextFactory.CreateDbContext();
        return await context.Trucks
                      .Include(t => t.Supplier)
                      .Where(t => t.ReceivedDate.Year == year)
                      .OrderByDescending(t => t.ReceivedDate)
                      .ToListAsync();
    }

    public async Task<Truck> GetTruckByIdAsync(int truckId)
    {
        await using var context = _contextFactory.CreateDbContext();
        return await context.Trucks
                      .Include(t => t.Supplier)
                      .FirstOrDefaultAsync(t => t.TruckID == truckId);
    }

    public async Task AddTruckAsync(Truck truck)
    {
        await using var context = _contextFactory.CreateDbContext();
        context.Trucks.Add(truck);
        await context.SaveChangesAsync();
    }

    public async Task UpdateTruckAsync(Truck truck)
    {
        await using var context = _contextFactory.CreateDbContext();
        context.Entry(truck).State = EntityState.Modified;
        await context.SaveChangesAsync();
    }

    public async Task DeleteTruckAsync(int truckId)
    {
        await using var context = _contextFactory.CreateDbContext();
        var truck = await context.Trucks.Include(t => t.TruckProducts).FirstOrDefaultAsync(t => t.TruckID == truckId);
        if (truck != null)
        {
            // First remove child records
            context.TruckProducts.RemoveRange(truck.TruckProducts);
            // Then remove the parent record
            context.Trucks.Remove(truck);
            await context.SaveChangesAsync();
        }
    }

    // --- TruckProducts CRUD Operations ---
    public async Task<List<TruckProducts>> GetTruckProductsAsync(int truckId)
    {
        await using var context = _contextFactory.CreateDbContext();
        return await context.TruckProducts
                      .Include(tp => tp.Product)
                      .Where(tp => tp.TruckID == truckId)
                      .ToListAsync();
    }

    public async Task AddTruckProductAsync(TruckProducts truckProduct)
    {
        await using var context = _contextFactory.CreateDbContext();
        context.TruckProducts.Add(truckProduct);
        await context.SaveChangesAsync();
    }

    public async Task UpdateTruckProductAsync(TruckProducts truckProduct)
    {
        await using var context = _contextFactory.CreateDbContext();
        context.Entry(truckProduct).State = EntityState.Modified;
        await context.SaveChangesAsync();
    }

    public async Task DeleteTruckProductAsync(int truckProductId)
    {
        await using var context = _contextFactory.CreateDbContext();
        var truckProduct = await context.TruckProducts.FindAsync(truckProductId);
        if (truckProduct != null)
        {
            context.TruckProducts.Remove(truckProduct);
            await context.SaveChangesAsync();
        }
    }
}
