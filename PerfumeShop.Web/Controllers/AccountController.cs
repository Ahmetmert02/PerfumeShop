
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using PerfumeShop.Web.Models;
using PerfumeShop.Web.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace PerfumeShop.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly IApiService _apiService;

        public AccountController(IConfiguration configuration, IHttpClientFactory httpClientFactory, IApiService apiService)
        {
            _configuration = configuration;
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri("http://localhost:5217/api/");
            Console.WriteLine($"Directly configured API URL: {_httpClient.BaseAddress}");
            _apiService = apiService;
        }

        public IActionResult Login()
        {
            // Setze die Anzahl der Artikel im Warenkorb auf 0 für nicht angemeldete Benutzer
            ViewBag.CartItemCount = 0;
            
            return View(new WebLoginModel());
        }

        [HttpPost]
        public async Task<IActionResult> Login(WebLoginModel model)
        {
            if (!ModelState.IsValid)
            {
                // Setze die Anzahl der Artikel im Warenkorb auf 0 bei ungültigen Eingaben
                ViewBag.CartItemCount = 0;
                return View(model);
            }

            try
            {
                Console.WriteLine("Starting login process...");
                Console.WriteLine($"API base URL: {_httpClient.BaseAddress}");

                // TEMPORARY SOLUTION: Direct admin access for testing purposes
                if (model.Email == "admin@perfumeshop.com" && model.Password == "admin123")
                {
                    Console.WriteLine("Admin user detected - direct access");
                    string adminToken;
                    
                    // Try to get a valid token from the API
                    try
                    {
                        Console.WriteLine("Attempting API login for admin...");
                        var adminApiModel = new
                        {
                            Email = "admin@perfumeshop.com",
                            Password = "admin123"
                        };
                        
                        var apiContent = new StringContent(JsonConvert.SerializeObject(adminApiModel), Encoding.UTF8, "application/json");
                        var apiResponse = await _httpClient.PostAsync("Auth/login", apiContent);
                        
                        if (apiResponse.IsSuccessStatusCode)
                        {
                            var responseContent = await apiResponse.Content.ReadAsStringAsync();
                            var responseObj = JsonConvert.DeserializeObject<dynamic>(responseContent);
                            var apiToken = responseObj?.token?.ToString();
                            
                            if (!string.IsNullOrEmpty(apiToken))
                            {
                                Console.WriteLine("Valid token obtained from API");
                                adminToken = apiToken;
                            }
                            else
                            {
                                // Generate a token with the same secret as the API
                                Console.WriteLine("API token was empty, generating own token");
                                adminToken = GenerateJwtToken();
                            }
                        }
                        else
                        {
                            // Generate a token with the same secret as the API
                            Console.WriteLine("API login failed, generating own token");
                            adminToken = GenerateJwtToken();
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error during API login: {ex.Message}");
                        Console.WriteLine("Generating own token");
                        
                        // Generate a token with the same secret as the API
                        adminToken = GenerateJwtToken();
                    }
                    
                    // Store token in cookie
                    Response.Cookies.Append("AuthToken", adminToken, new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = false,
                        SameSite = SameSiteMode.Lax,
                        Expires = DateTime.Now.AddHours(24) // Longer validity
                    });
                    
                    Console.WriteLine("Admin token stored in cookie");
                    
                    // Create claims for the admin user
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, "1"),
                        new Claim(ClaimTypes.Email, "admin@perfumeshop.com"),
                        new Claim(ClaimTypes.GivenName, "Admin"),
                        new Claim(ClaimTypes.Surname, "User"),
                        new Claim(ClaimTypes.Role, "Admin")
                    };

                    // Create identity and principal
                    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var principal = new ClaimsPrincipal(identity);

                    // Sign in the user
                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, new AuthenticationProperties
                    {
                        IsPersistent = model.RememberMe,
                        ExpiresUtc = DateTime.UtcNow.AddHours(24)
                    });

                    // Store user info in session
                    var userSession = new UserSessionModel
                    {
                        UserId = 1,
                        Email = "admin@perfumeshop.com",
                        FirstName = "Admin",
                        LastName = "User",
                        IsAdmin = true,
                        IsAuthenticated = true,
                        Token = adminToken
                    };

                    HttpContext.Session.SetString("UserSession", JsonConvert.SerializeObject(userSession));
                    
                    // Hole Warenkorb-Informationen für die Anzeige des Zählers
                    await UpdateCartItemCount();
                    
                    Console.WriteLine("Admin session created - redirecting to admin area");
                    return RedirectToAction("Index", "Home", new { area = "Admin" });
                }

                // Normal login process for non-admin users
                var apiModel = new
                {
                    Email = model.Email,
                    Password = model.Password
                };

                var content = new StringContent(JsonConvert.SerializeObject(apiModel), Encoding.UTF8, "application/json");
                Console.WriteLine($"Sending request to: {_httpClient.BaseAddress}Auth/login");
                
                var response = await _httpClient.PostAsync("Auth/login", content);
                Console.WriteLine($"Response received: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    
                    // Extract token and other data from API response
                    var responseObj = JsonConvert.DeserializeObject<dynamic>(responseContent);
                    
                    // Debug output of properties
                    Console.WriteLine("==== API Response Properties ====");
                    foreach (var prop in ((Newtonsoft.Json.Linq.JObject)responseObj).Properties())
                    {
                        Console.WriteLine($"Property: {prop.Name} = {prop.Value}");
                    }
                    
                    // The token is returned with lowercase "t"
                    var token = responseObj?.token?.ToString();
                    
                    Console.WriteLine($"Extracted token: {token}");
                    Console.WriteLine($"Full API response: {responseContent}");
                    
                    // Create WebAuthResponseModel with data from API response
                    var authResponse = new WebAuthResponseModel
                    {
                        Success = true,
                        Message = responseObj?.Message?.ToString(),
                        UserId = responseObj?.userId != null ? Convert.ToInt32(responseObj.userId) : 0,
                        Email = responseObj?.email?.ToString(),
                        FirstName = responseObj?.firstName?.ToString(),
                        LastName = responseObj?.lastName?.ToString(),
                        IsAdmin = responseObj?.isAdmin != null ? Convert.ToBoolean(responseObj.isAdmin) : false,
                        Token = token,
                        Expiration = responseObj?.expiration != null ? Convert.ToDateTime(responseObj.expiration) : DateTime.Now.AddHours(1)
                    };

                    if (authResponse == null)
                    {
                        ModelState.AddModelError("", "Authentication error");
                        // Setze die Anzahl der Artikel im Warenkorb auf 0 bei Authentifizierungsfehler
                        ViewBag.CartItemCount = 0;
                        return View(model);
                    }

                    // Store token in a cookie
                    if (!string.IsNullOrEmpty(authResponse.Token))
                    {
                        Response.Cookies.Append("AuthToken", authResponse.Token, new CookieOptions
                        {
                            HttpOnly = true,
                            // Set Secure to false in development mode
                            Secure = false,
                            SameSite = SameSiteMode.Lax,
                            Expires = DateTime.Now.AddHours(1)
                        });
                        Console.WriteLine("Token stored in cookie");
                        Console.WriteLine($"Stored token: {authResponse.Token}");
                    }
                    else
                    {
                        Console.WriteLine("No token to store!");
                    }

                    // Create claims for the user
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, authResponse.UserId.ToString()),
                        new Claim(ClaimTypes.Email, authResponse.Email),
                        new Claim(ClaimTypes.GivenName, authResponse.FirstName),
                        new Claim(ClaimTypes.Surname, authResponse.LastName)
                    };

                    // Add role claim
                    if (authResponse.IsAdmin)
                    {
                        claims.Add(new Claim(ClaimTypes.Role, "Admin"));
                        Console.WriteLine("Admin role added");
                    }
                    else
                    {
                        claims.Add(new Claim(ClaimTypes.Role, "User"));
                        Console.WriteLine("User role added");
                    }
                    
                    Console.WriteLine($"IsAdmin value: {authResponse.IsAdmin}");
                    Console.WriteLine($"Claims created: {string.Join(", ", claims.Select(c => $"{c.Type}={c.Value}"))}");

                    // Create identity and principal
                    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var principal = new ClaimsPrincipal(identity);

                    // Sign in the user
                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, new AuthenticationProperties
                    {
                        IsPersistent = model.RememberMe,
                        ExpiresUtc = DateTime.UtcNow.AddHours(1)
                    });

                    // Store user info in session
                    var userSession = new UserSessionModel
                    {
                        UserId = authResponse.UserId,
                        Email = authResponse.Email,
                        FirstName = authResponse.FirstName,
                        LastName = authResponse.LastName,
                        IsAdmin = authResponse.IsAdmin,
                        IsAuthenticated = true,
                        Token = authResponse.Token
                    };

                    HttpContext.Session.SetString("UserSession", JsonConvert.SerializeObject(userSession));
                    
                    // Hole Warenkorb-Informationen für die Anzeige des Zählers
                    await UpdateCartItemCount();

                    if (authResponse.IsAdmin)
                    {
                        Console.WriteLine("Redirecting to admin area");
                        return RedirectToAction("Index", "Home", new { area = "Admin" });
                    }
                    else
                    {
                        Console.WriteLine("Redirecting to home page (no admin)");
                        return RedirectToAction("Index", "Home");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Login error: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                    Console.WriteLine($"Inner Exception Stack Trace: {ex.InnerException.StackTrace}");
                }
                
                ModelState.AddModelError("", $"Error connecting to API: {ex.Message}");
                
                // Setze die Anzahl der Artikel im Warenkorb auf 0 bei Fehlern
                ViewBag.CartItemCount = 0;
                return View(model);
            }

            ModelState.AddModelError("", "Invalid email or password");
            // Setze die Anzahl der Artikel im Warenkorb auf 0 bei ungültigen Anmeldedaten
            ViewBag.CartItemCount = 0;
            return View(model);
        }

        public IActionResult Register()
        {
            // Setze die Anzahl der Artikel im Warenkorb auf 0 für nicht angemeldete Benutzer
            ViewBag.CartItemCount = 0;
            
            return View(new WebRegisterModel());
        }

        [HttpPost]
        public async Task<IActionResult> Register(WebRegisterModel model)
        {
            if (!ModelState.IsValid)
            {
                // Setze die Anzahl der Artikel im Warenkorb auf 0 bei ungültigen Eingaben
                ViewBag.CartItemCount = 0;
                return View(model);
            }

            try
            {
                Console.WriteLine("Starting registration process...");
                
                // Convert to API model
                var apiModel = new
                {
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Email = model.Email,
                    Password = model.Password,
                    ConfirmPassword = model.ConfirmPassword,
                    Address = model.Address
                };

                var content = new StringContent(JsonConvert.SerializeObject(apiModel), Encoding.UTF8, "application/json");
                Console.WriteLine($"Sending request to: {_httpClient.BaseAddress}Auth/register");
                
                var response = await _httpClient.PostAsync("Auth/register", content);
                Console.WriteLine($"Response received: {response.StatusCode}");
                
                var responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Response content: {responseContent}");

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "Registration successful! You can now log in.";
                    return RedirectToAction("Login");
                }
                else
                {
                    // Try to extract error message from API response
                    try
                    {
                        var errorResponse = JsonConvert.DeserializeObject<dynamic>(responseContent);
                        var errorMessage = errorResponse?.Message?.ToString() ?? "Registration failed.";
                        ModelState.AddModelError("", errorMessage);
                    }
                    catch
                    {
                        ModelState.AddModelError("", "Registration failed. Please try again.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during registration: {ex.Message}");
                ModelState.AddModelError("", $"Error connecting to API: {ex.Message}");
            }
            
            // Setze die Anzahl der Artikel im Warenkorb auf 0 bei Fehlern
            ViewBag.CartItemCount = 0;
            return View(model);
        }

        public async Task<IActionResult> Logout()
        {
            // Sign out the user
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            
            // Remove session data
            HttpContext.Session.Clear();
            
            // Remove auth token cookie
            Response.Cookies.Delete("AuthToken");
            
            // Setze die Anzahl der Artikel im Warenkorb auf 0 nach dem Logout
            ViewBag.CartItemCount = 0;
            
            return RedirectToAction("Index", "Home");
        }
        
        public async Task<IActionResult> AccessDenied()
        {
            // Hole Warenkorb-Informationen für die Anzeige des Zählers
            await UpdateCartItemCount();
            
            return View();
        }

        // New method to generate a JWT token with the correct secret
        private string GenerateJwtToken()
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(ClaimTypes.Email, "admin@perfumeshop.com"),
                new Claim(ClaimTypes.GivenName, "Admin"),
                new Claim(ClaimTypes.Role, "Admin")
            };

            var secret = _configuration["JWT:Secret"];
            Console.WriteLine($"Using JWT Secret: {secret}");
            
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["JWT:ValidIssuer"],
                audience: _configuration["JWT:ValidAudience"],
                claims: claims,
                expires: DateTime.Now.AddDays(7), // Longer validity for testing purposes
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
        
        // Hilfsmethode zum Aktualisieren des Warenkorb-Zählers
        private async Task UpdateCartItemCount()
        {
            try
            {
                // Prüfe, ob der Benutzer angemeldet ist
                var userSessionJson = HttpContext.Session.GetString("UserSession");
                if (!string.IsNullOrEmpty(userSessionJson))
                {
                    // Hole Warenkorb-Informationen
                    var shoppingCartResponse = await _apiService.GetShoppingCartAsync();
                    ViewBag.CartItemCount = shoppingCartResponse?.TotalItems ?? 0;
                }
                else
                {
                    ViewBag.CartItemCount = 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fehler beim Abrufen des Warenkorb-Zählers: {ex.Message}");
                ViewBag.CartItemCount = 0;
            }
        }
    }
} 