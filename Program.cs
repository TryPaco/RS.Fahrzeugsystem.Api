using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using RS.Fahrzeugsystem.Api.Authorization;
using RS.Fahrzeugsystem.Api.Data;
using RS.Fahrzeugsystem.Api.Options;
using RS.Fahrzeugsystem.Api.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<JwtOptions>(
	builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.Configure<SmtpOptions>(
	builder.Configuration.GetSection(SmtpOptions.SectionName));
builder.Services.Configure<PasswordResetOptions>(
	builder.Configuration.GetSection(PasswordResetOptions.SectionName));

builder.Services.AddDbContext<AppDbContext>(options =>
	options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IEmailService, SmtpEmailService>();
builder.Services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
builder.Services.AddScoped<IVinDecoderService, VinDecoderService>();

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
				 ?? throw new InvalidOperationException("JWT settings missing.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
	.AddJwtBearer(options =>
	{
		options.TokenValidationParameters = new TokenValidationParameters
		{
			ValidateIssuer = true,
			ValidateAudience = true,
			ValidateLifetime = true,
			ValidateIssuerSigningKey = true,
			ValidIssuer = jwtOptions.Issuer,
			ValidAudience = jwtOptions.Audience,
			IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),
			ClockSkew = TimeSpan.FromMinutes(1)
		};
	});

builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
	options.AddPolicy("frontend", policy =>
	{
		policy
			.WithOrigins(
				"http://localhost:5173",
				"https://mizamidis.eu",
				"https://www.mizamidis.eu"
			)
			.AllowAnyHeader()
			.AllowAnyMethod();
	});
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
	options.SwaggerDoc("v1", new OpenApiInfo
	{
		Title = "RS Fahrzeugsystem API",
		Version = "v1"
	});

	options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
	{
		Name = "Authorization",
		Type = SecuritySchemeType.Http,
		Scheme = "bearer",
		BearerFormat = "JWT",
		In = ParameterLocation.Header,
		Description = "JWT Token eingeben. Beispiel: Bearer {token}"
	});

	options.AddSecurityRequirement(new OpenApiSecurityRequirement
	{
		{
			new OpenApiSecurityScheme
			{
				Reference = new OpenApiReference
				{
					Type = ReferenceType.SecurityScheme,
					Id = "Bearer"
				}
			},
			Array.Empty<string>()
		}
	});
});

var app = builder.Build();

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
	ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

app.UseCors("frontend");

using (var scope = app.Services.CreateScope())
{
	var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
	await SeedData.EnsureSeededAsync(dbContext);
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
