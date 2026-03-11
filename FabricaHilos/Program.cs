var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Oracle DB Context
builder.Services.AddSingleton<FabricaHilos.Data.OracleDbContext>();

// Repositories
builder.Services.AddScoped<FabricaHilos.Data.Interfaces.IHiloRepository,
    FabricaHilos.Data.Repositories.HiloRepository>();
builder.Services.AddScoped<FabricaHilos.Data.Interfaces.IMaterialRepository,
    FabricaHilos.Data.Repositories.MaterialRepository>();
builder.Services.AddScoped<FabricaHilos.Data.Interfaces.IClienteRepository,
    FabricaHilos.Data.Repositories.ClienteRepository>();
builder.Services.AddScoped<FabricaHilos.Data.Interfaces.IPedidoRepository,
    FabricaHilos.Data.Repositories.PedidoRepository>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
