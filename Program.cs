
using Microsoft.EntityFrameworkCore;
using ecommerce.Context;
using Scalar.AspNetCore;
using System.Text;
using ecommerce_back.Services;

var builder = WebApplication.CreateBuilder(args);

// .net 9 only
// builder.Services.AddOpenApi();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// builder.Services.AddAuthentication(options =>
//     {
//       options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
//       options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
//       options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
//     }
//  )
//    .AddJwtBearer(options =>
//      {
//          options.SaveToken = true;
//          options.RequireHttpsMetadata = false;
//          options.TokenValidationParameters = new TokenValidationParameters
//          {
//              ValidateIssuer = true,
//              ValidateAudience = true,
//              ValidAudience = builder.Configuration["JWT:Audience"],
//              ValidIssuer = builder.Configuration["JWT:Issuer"],
//              ClockSkew = TimeSpan.Zero,
//              IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JWT:SecretKey"]))
//          };
//      }
//     );

builder.Services.AddScoped<ITokenService, TokenService>();
// builder.Services.AddAuthorization();
builder.Services.AddAWSLambdaHosting(LambdaEventSource.HttpApi);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<ApplicationDbContext>(
    options => options.UseNpgsql(connectionString, o => o.EnableRetryOnFailure())
);

var app = builder.Build();

// .net 9 only
// app.MapOpenApi();

app.MapControllers();
// app.MapScalarApiReference();

app.UseSwagger();
app.UsePathBase("/Prod");

// app.UseAuthentication();
// app.UseAuthorization();

app.Run();