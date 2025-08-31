// Services/TruckDataService.cs

using Microsoft.EntityFrameworkCore;
using StandOrder.Models; // Your models namespace

public class TruckDataService
{
    private readonly AppDbContext _context;

    public TruckDataService(AppDbContext context)
    {
        _context = context;
    }

    // --- Lookup Methods ---
    public async Task<List<Supplier>> GetSuppliersAsync() =>
        await _context.Suppliers.OrderBy(s => s.SupplierName).ToListAsync();

    public async Task<List<Product>> GetProductsAsync() =>
        await _context.Products.OrderBy(p => p.ItemNumber).ToListAsync();

    // --- Truck CRUD Operations ---
    public async Task<List<Truck>> GetTrucksByYearAsync(int year) =>
        await _context.Trucks
                      .Include(t => t.Supplier)
                      .Where(t => t.ReceivedDate.Year == year)
                      .OrderByDescending(t => t.ReceivedDate)
                      .ToListAsync();

    public async Task<Truck> GetTruckByIdAsync(int truckId) =>
        await _context.Trucks
                      .Include(t => t.Supplier)
                      .FirstOrDefaultAsync(t => t.TruckID == truckId);

    public async Task AddTruckAsync(Truck truck)
    {
        _context.Trucks.Add(truck);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateTruckAsync(Truck truck)
    {
        _context.Entry(truck).State = EntityState.Modified;
        await _context.SaveChangesAsync();
    }

    public async Task DeleteTruckAsync(int truckId)
    {
        var truck = await _context.Trucks.Include(t => t.TruckProducts).FirstOrDefaultAsync(t => t.TruckID == truckId);
        if (truck != null)
        {
            // First remove child records
            _context.TruckProducts.RemoveRange(truck.TruckProducts);
            // Then remove the parent record
            _context.Trucks.Remove(truck);
            await _context.SaveChangesAsync();
        }
    }

    // --- TruckProducts CRUD Operations ---
    public async Task<List<TruckProducts>> GetTruckProductsAsync(int truckId) =>
        await _context.TruckProducts
                      .Include(tp => tp.Product)
                      .Where(tp => tp.TruckID == truckId)
                      .ToListAsync();

    public async Task AddTruckProductAsync(TruckProducts truckProduct)
    {
        _context.TruckProducts.Add(truckProduct);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateTruckProductAsync(TruckProducts truckProduct)
    {
        _context.Entry(truckProduct).State = EntityState.Modified;
        await _context.SaveChangesAsync();
    }

    public async Task DeleteTruckProductAsync(int truckProductId)
    {
        var truckProduct = await _context.TruckProducts.FindAsync(truckProductId);
        if (truckProduct != null)
        {
            _context.TruckProducts.Remove(truckProduct);
            await _context.SaveChangesAsync();
        }
    }
}