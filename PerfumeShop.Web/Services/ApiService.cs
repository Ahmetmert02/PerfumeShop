using Newtonsoft.Json;
using PerfumeShop.Core.Entities;
using PerfumeShop.Web.Models;
using System.Text;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace PerfumeShop.Web.Services
{
    public class ApiService : IApiService
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ApiService(HttpClient httpClient, IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClient;
            _httpContextAccessor = httpContextAccessor;
        }

        // Neue Methode zum Hinzufügen des Authorization-Headers
        private void AddAuthorizationHeader()
        {
            try
            {
                // Token aus dem Cookie holen
                var token = _httpContextAccessor.HttpContext?.Request.Cookies["AuthToken"];
                
                Console.WriteLine($"Token aus Cookie: {token}");
                
                // Falls kein Token im Cookie, versuche ihn aus der Session zu holen
                if (string.IsNullOrEmpty(token) && _httpContextAccessor.HttpContext != null)
                {
                    var sessionString = _httpContextAccessor.HttpContext.Session.GetString("UserSession");
                    if (!string.IsNullOrEmpty(sessionString))
                    {
                        var userSession = JsonConvert.DeserializeObject<UserSessionModel>(sessionString);
                        token = userSession?.Token;
                        Console.WriteLine($"Token aus Session geholt: {token}");
                    }
                }
                
                if (!string.IsNullOrEmpty(token))
                {
                    // Bestehende Header entfernen und neuen hinzufügen
                    if (_httpClient.DefaultRequestHeaders.Contains("Authorization"))
                    {
                        _httpClient.DefaultRequestHeaders.Remove("Authorization");
                    }
                    
                    // Hier verwenden wir die korrekte Methode zum Hinzufügen des Authorization-Headers
                    _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"Bearer {token}");
                    
                    Console.WriteLine("Authorization-Header hinzugefügt");
                    Console.WriteLine($"Header-Wert: {_httpClient.DefaultRequestHeaders.GetValues("Authorization").FirstOrDefault()}");
                    
                    // Überprüfe, ob der Token gültig ist
                    var tokenParts = token.Split('.');
                    if (tokenParts.Length == 3) // Ein JWT-Token besteht aus 3 Teilen
                    {
                        Console.WriteLine("Token hat gültiges Format");
                        
                        // Extrahiere Claims aus dem Token (nur für Debug-Zwecke)
                        try
                        {
                            var payload = tokenParts[1];
                            // Padding hinzufügen, falls erforderlich
                            while (payload.Length % 4 != 0)
                            {
                                payload += "=";
                            }
                            
                            var base64Decoded = System.Convert.FromBase64String(payload.Replace('-', '+').Replace('_', '/'));
                            var jsonPayload = System.Text.Encoding.UTF8.GetString(base64Decoded);
                            Console.WriteLine($"Token Payload: {jsonPayload}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Fehler beim Decodieren des Tokens: {ex.Message}");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Token hat ungültiges Format!");
                    }
                }
                else
                {
                    Console.WriteLine("Kein Token im Cookie oder in der Session gefunden!");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fehler beim Hinzufügen des Authorization-Headers: {ex.Message}");
            }
        }

        // Generic API method implementation
        public async Task<T> GetAsync<T>(string endpoint) where T : class
        {
            try
            {
                AddAuthorizationHeader();
                
                var response = await _httpClient.GetAsync(endpoint);
                
                // Überprüfe auf 403 Forbidden und behandle es speziell
                if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    Console.WriteLine($"403 Forbidden bei Anfrage an {endpoint}");
                    return null;
                }
                
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<T>(content) ?? throw new Exception($"Failed to deserialize response from {endpoint}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetAsync<{typeof(T).Name}>: {ex.Message}");
                throw;
            }
        }

        public async Task<IEnumerable<Product>> GetProductsAsync()
        {
            var response = await _httpClient.GetAsync("Products");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<IEnumerable<Product>>(content) ?? Enumerable.Empty<Product>();
        }

        public async Task<Product?> GetProductByIdAsync(int id)
        {
            var response = await _httpClient.GetAsync($"Products/{id}");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<Product>(content);
        }

        public async Task<bool> CreateProductAsync(Product product)
        {
            try
            {
                // Authentifizierungsheader mit dem dynamischen Token aus dem Cookie hinzufügen
                AddAuthorizationHeader();
                
                // Debug-Ausgabe für die Request-Header und API-URL
                Console.WriteLine($"API BaseAddress: {_httpClient.BaseAddress}");
                Console.WriteLine("Request-Headers für Produkterstellung:");
                foreach (var header in _httpClient.DefaultRequestHeaders)
                {
                    Console.WriteLine($"{header.Key}: {string.Join(", ", header.Value)}");
                }
                
                // Erstelle ein vereinfachtes Objekt für die API, das dem CreateProductDto entspricht
                var productDto = new
                {
                    Name = product.Name,
                    Description = product.Description,
                    Price = product.Price,
                    Stock = product.Stock,
                    ImageUrl = product.ImageUrl,
                    CategoryId = product.CategoryId,
                    BrandId = product.BrandId
                };
                
                Console.WriteLine($"Sende Produkt: {JsonConvert.SerializeObject(productDto)}");
                
                var content = new StringContent(JsonConvert.SerializeObject(productDto), Encoding.UTF8, "application/json");
                
                // Setze explizit den Content-Type-Header
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                
                // Versuche mit Timeout und Verbindungshandling
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(10));
                var requestTask = _httpClient.PostAsync("Products", content);
                
                var completedTask = await Task.WhenAny(requestTask, timeoutTask);
                
                if (completedTask == timeoutTask)
                {
                    Console.WriteLine("API-Anfrage Timeout nach 10 Sekunden");
                    return false;
                }
                
                var response = await requestTask;
                
                // Debug-Ausgabe für die Antwort
                Console.WriteLine($"Status Code: {response.StatusCode}");
                Console.WriteLine($"Response Headers: {string.Join(", ", response.Headers.Select(h => $"{h.Key}: {string.Join(", ", h.Value)}"))}");
                
                var responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Response Content: {responseContent}");
                
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Error Content: {responseContent}");
                }
                
                return response.IsSuccessStatusCode;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"HttpRequestException beim API-Aufruf: {ex.Message}");
                Console.WriteLine($"Status Code: {ex.StatusCode}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                
                // Überprüfe, ob die API erreichbar ist
                try
                {
                    Console.WriteLine("Überprüfe API-Erreichbarkeit...");
                    var pingTask = _httpClient.GetAsync("");
                    var pingTimeoutTask = Task.Delay(TimeSpan.FromSeconds(5));
                    
                    var pingCompleted = await Task.WhenAny(pingTask, pingTimeoutTask);
                    if (pingCompleted == pingTimeoutTask)
                    {
                        Console.WriteLine("API scheint nicht erreichbar zu sein. Bitte stellen Sie sicher, dass der API-Server läuft.");
                    }
                }
                catch (Exception pingEx)
                {
                    Console.WriteLine($"API ist nicht erreichbar: {pingEx.Message}");
                }
                
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Allgemeine Exception beim API-Aufruf: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                    Console.WriteLine($"Inner Exception StackTrace: {ex.InnerException.StackTrace}");
                }
                return false;
            }
        }

        public async Task<bool> UpdateProductAsync(int id, Product product)
        {
            try
            {
                AddAuthorizationHeader();
                
                // Debug-Ausgabe für die Request-Header und API-URL
                Console.WriteLine($"API BaseAddress: {_httpClient.BaseAddress}");
                Console.WriteLine("Request-Headers für Produktaktualisierung:");
                foreach (var header in _httpClient.DefaultRequestHeaders)
                {
                    Console.WriteLine($"{header.Key}: {string.Join(", ", header.Value)}");
                }
                
                // Erstelle ein vereinfachtes Objekt für die API, das dem UpdateProductDto entspricht
                var productDto = new
                {
                    Id = product.Id,
                    Name = product.Name,
                    Description = product.Description,
                    Price = product.Price,
                    Stock = product.Stock,
                    ImageUrl = product.ImageUrl,
                    IsActive = product.IsActive,
                    CategoryId = product.CategoryId,
                    BrandId = product.BrandId
                };
                
                Console.WriteLine($"Sende Produkt-Update: {JsonConvert.SerializeObject(productDto)}");
                
                var content = new StringContent(JsonConvert.SerializeObject(productDto), Encoding.UTF8, "application/json");
                
                // Setze explizit den Content-Type-Header
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                
                // Versuche mit Timeout und Verbindungshandling
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(10));
                var requestTask = _httpClient.PutAsync($"Products/{id}", content);
                
                var completedTask = await Task.WhenAny(requestTask, timeoutTask);
                
                if (completedTask == timeoutTask)
                {
                    Console.WriteLine("API-Anfrage Timeout nach 10 Sekunden");
                    return false;
                }
                
                var response = await requestTask;
                
                // Debug-Ausgabe für die Antwort
                Console.WriteLine($"Status Code: {response.StatusCode}");
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Update Error Content: {errorContent}");
                }
                
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception beim API-Aufruf (Update): {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                    Console.WriteLine($"Inner Exception StackTrace: {ex.InnerException.StackTrace}");
                }
                return false;
            }
        }

        public async Task<bool> DeleteProductAsync(int id)
        {
            AddAuthorizationHeader();
            var response = await _httpClient.DeleteAsync($"Products/{id}");
            return response.IsSuccessStatusCode;
        }

        public async Task<WebAuthResponseModel> LoginAsync(string email, string password)
        {
            var loginModel = new { Email = email, Password = password };
            var content = new StringContent(JsonConvert.SerializeObject(loginModel), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("Auth/login", content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<WebAuthResponseModel>(responseContent) ?? new WebAuthResponseModel { Success = false, Message = "Fehler beim Deserialisieren der Antwort" };
            }
            
            return new WebAuthResponseModel { Success = false, Message = "Login fehlgeschlagen" };
        }

        public async Task<bool> RegisterAsync(User user)
        {
            var content = new StringContent(JsonConvert.SerializeObject(user), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("Auth/register", content);
            return response.IsSuccessStatusCode;
        }

        public async Task<IEnumerable<Category>> GetCategoriesAsync()
        {
            var response = await _httpClient.GetAsync("Categories");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<IEnumerable<Category>>(content) ?? Enumerable.Empty<Category>();
        }

        public async Task<Category?> GetCategoryByIdAsync(int id)
        {
            var response = await _httpClient.GetAsync($"Categories/{id}");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<Category>(content);
        }

        public async Task<bool> CreateCategoryAsync(Category category)
        {
            try
            {
                // Authentifizierungsheader mit dem dynamischen Token aus dem Cookie hinzufügen
                AddAuthorizationHeader();
                
                // Debug-Ausgabe für die Request-Header und API-URL
                Console.WriteLine($"API BaseAddress: {_httpClient.BaseAddress}");
                Console.WriteLine("Request-Headers für Kategorieerstellung:");
                foreach (var header in _httpClient.DefaultRequestHeaders)
                {
                    Console.WriteLine($"{header.Key}: {string.Join(", ", header.Value)}");
                }
                
                // Erstelle ein vereinfachtes Objekt für die API
                var categoryDto = new
                {
                    Name = category.Name ?? string.Empty,
                    Description = category.Description,
                    IsActive = category.IsActive
                };
                
                Console.WriteLine($"Sende Kategorie: {JsonConvert.SerializeObject(categoryDto)}");
                
                var content = new StringContent(JsonConvert.SerializeObject(categoryDto), Encoding.UTF8, "application/json");
                
                // Setze explizit den Content-Type-Header
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                
                // Versuche mit Timeout und Verbindungshandling
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(10));
                var requestTask = _httpClient.PostAsync("Categories", content);
                
                var completedTask = await Task.WhenAny(requestTask, timeoutTask);
                
                if (completedTask == timeoutTask)
                {
                    Console.WriteLine("API-Anfrage Timeout nach 10 Sekunden");
                    return false;
                }
                
                var response = await requestTask;
                
                // Debug-Ausgabe für die Antwort
                Console.WriteLine($"Status Code: {response.StatusCode}");
                Console.WriteLine($"Response Headers: {string.Join(", ", response.Headers.Select(h => $"{h.Key}: {string.Join(", ", h.Value)}"))}");
                
                var responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Response Content: {responseContent}");
                
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Error Content: {responseContent}");
                }
                
                return response.IsSuccessStatusCode;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"HttpRequestException beim API-Aufruf: {ex.Message}");
                Console.WriteLine($"Status Code: {ex.StatusCode}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                
                // Überprüfe, ob die API erreichbar ist
                try
                {
                    Console.WriteLine("Überprüfe API-Erreichbarkeit...");
                    var pingTask = _httpClient.GetAsync("");
                    var pingTimeoutTask = Task.Delay(TimeSpan.FromSeconds(5));
                    
                    var pingCompleted = await Task.WhenAny(pingTask, pingTimeoutTask);
                    if (pingCompleted == pingTimeoutTask)
                    {
                        Console.WriteLine("API scheint nicht erreichbar zu sein. Bitte stellen Sie sicher, dass der API-Server läuft.");
                    }
                }
                catch (Exception pingEx)
                {
                    Console.WriteLine($"API ist nicht erreichbar: {pingEx.Message}");
                }
                
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Allgemeine Exception beim API-Aufruf: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                    Console.WriteLine($"Inner Exception StackTrace: {ex.InnerException.StackTrace}");
                }
                return false;
            }
        }

        public async Task<bool> UpdateCategoryAsync(int id, Category category)
        {
            try
            {
                AddAuthorizationHeader();
                
                // Erstelle ein vereinfachtes Objekt für die API, das den DTO-Anforderungen entspricht
                var categoryDto = new
                {
                    Id = category.Id,
                    Name = category.Name,
                    Description = category.Description,
                    IsActive = category.IsActive
                };
                
                var content = new StringContent(JsonConvert.SerializeObject(categoryDto), Encoding.UTF8, "application/json");
                var response = await _httpClient.PutAsync($"Categories/{id}", content);
                
                // Debug-Ausgabe für die Antwort
                Console.WriteLine($"Update Status Code: {response.StatusCode}");
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Update Error Content: {errorContent}");
                }
                
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception beim API-Aufruf (Update): {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeleteCategoryAsync(int id)
        {
            AddAuthorizationHeader();
            var response = await _httpClient.DeleteAsync($"Categories/{id}");
            return response.IsSuccessStatusCode;
        }
        
        // Brands
        public async Task<IEnumerable<Brand>> GetBrandsAsync()
        {
            var response = await _httpClient.GetAsync("Brands");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<IEnumerable<Brand>>(content) ?? Enumerable.Empty<Brand>();
        }

        public async Task<Brand?> GetBrandByIdAsync(int id)
        {
            var response = await _httpClient.GetAsync($"Brands/{id}");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<Brand>(content);
        }

        public async Task<bool> CreateBrandAsync(Brand brand)
        {
            try
            {
                AddAuthorizationHeader();
                
                // Erstelle ein vereinfachtes Objekt für die API
                var brandDto = new
                {
                    Name = brand.Name ?? string.Empty,
                    Description = brand.Description,
                    IsActive = brand.IsActive
                };
                
                var content = new StringContent(JsonConvert.SerializeObject(brandDto), Encoding.UTF8, "application/json");
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(10));
                var requestTask = _httpClient.PostAsync("Brands", content);
                
                var completedTask = await Task.WhenAny(requestTask, timeoutTask);
                
                if (completedTask == timeoutTask)
                {
                    Console.WriteLine("API-Anfrage Timeout nach 10 Sekunden");
                    return false;
                }
                
                var response = await requestTask;
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Error Content: {errorContent}");
                }
                
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception beim API-Aufruf: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UpdateBrandAsync(int id, Brand brand)
        {
            try
            {
                AddAuthorizationHeader();
                
                var brandDto = new
                {
                    Id = brand.Id,
                    Name = brand.Name,
                    Description = brand.Description,
                    IsActive = brand.IsActive
                };
                
                var content = new StringContent(JsonConvert.SerializeObject(brandDto), Encoding.UTF8, "application/json");
                var response = await _httpClient.PutAsync($"Brands/{id}", content);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Update Error Content: {errorContent}");
                }
                
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception beim API-Aufruf (Update): {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeleteBrandAsync(int id)
        {
            AddAuthorizationHeader();
            var response = await _httpClient.DeleteAsync($"Brands/{id}");
            return response.IsSuccessStatusCode;
        }
        
        // Orders
        public async Task<IEnumerable<Order>> GetOrdersAsync()
        {
            AddAuthorizationHeader();
            var response = await _httpClient.GetAsync("Orders");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<IEnumerable<Order>>(content) ?? Enumerable.Empty<Order>();
        }

        public async Task<Order?> GetOrderByIdAsync(int id)
        {
            AddAuthorizationHeader();
            var response = await _httpClient.GetAsync($"Orders/{id}");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<Order>(content);
        }

        public async Task<IEnumerable<Order>> GetUserOrdersAsync(int userId)
        {
            AddAuthorizationHeader();
            var response = await _httpClient.GetAsync($"Orders/User/{userId}");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<IEnumerable<Order>>(content) ?? Enumerable.Empty<Order>();
        }

        public async Task<Order> CreateOrderAsync(Order order)
        {
            try
            {
                AddAuthorizationHeader();
                
                // Debug-Ausgabe für die Request-Header und API-URL
                Console.WriteLine($"API BaseAddress: {_httpClient.BaseAddress}");
                Console.WriteLine("Request-Headers für Bestellerstellung:");
                foreach (var header in _httpClient.DefaultRequestHeaders)
                {
                    Console.WriteLine($"{header.Key}: {string.Join(", ", header.Value)}");
                }
                
                // Erstelle ein CreateOrderDto für die API, das keine User-Eigenschaft hat
                var orderDto = new
                {
                    OrderNumber = order.OrderNumber,
                    OrderDate = order.OrderDate,
                    Status = (int)order.Status,
                    TotalAmount = order.TotalAmount,
                    UserId = order.UserId,
                    ShippingAddress = order.ShippingAddress,
                    PaymentMethod = order.PaymentMethod,
                    OrderItems = order.OrderItems.Select(oi => new
                    {
                        ProductId = oi.ProductId,
                        Quantity = oi.Quantity,
                        UnitPrice = oi.UnitPrice,
                        TotalPrice = oi.TotalPrice
                    }).ToList()
                };
                
                Console.WriteLine($"Sende Bestellung: {JsonConvert.SerializeObject(orderDto)}");
                
                var content = new StringContent(JsonConvert.SerializeObject(orderDto), Encoding.UTF8, "application/json");
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                
                var response = await _httpClient.PostAsync("Orders", content);
                
                // Debug-Ausgabe für die Antwort
                Console.WriteLine($"Status Code: {response.StatusCode}");
                
                var responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Response Content: {responseContent}");
                
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Error Content: {responseContent}");
                    throw new HttpRequestException($"Fehler beim Erstellen der Bestellung: {response.StatusCode} - {responseContent}");
                }
                
                return JsonConvert.DeserializeObject<Order>(responseContent) ?? throw new Exception("Fehler beim Deserialisieren der Bestellung");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception beim API-Aufruf (CreateOrder): {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }
                throw; // Werfe die Exception weiter, damit sie im Controller behandelt werden kann
            }
        }

        public async Task<bool> UpdateOrderStatusAsync(int id, OrderStatus status)
        {
            AddAuthorizationHeader();
            var content = new StringContent(JsonConvert.SerializeObject(status), Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"Orders/{id}/status", content);
            return response.IsSuccessStatusCode;
        }

        // Shopping Cart
        public async Task<ShoppingCartResponse> GetShoppingCartAsync()
        {
            try
            {
                AddAuthorizationHeader();
                
                var response = await _httpClient.GetAsync("ShoppingCarts");
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<ShoppingCartResponse>(responseContent) ?? new ShoppingCartResponse();
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Fehler beim Abrufen des Warenkorbs: {errorContent}");
                    return new ShoppingCartResponse();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception beim Abrufen des Warenkorbs: {ex.Message}");
                return new ShoppingCartResponse();
            }
        }

        public async Task<ShoppingCart> CreateShoppingCartAsync(ShoppingCart shoppingCart)
        {
            try
            {
                AddAuthorizationHeader();
                
                // Erstelle ein vereinfachtes DTO-Objekt, das nur die erforderlichen Felder enthält
                var shoppingCartDto = new
                {
                    UserId = shoppingCart.UserId,
                    LastModified = DateTime.Now,
                    CartItems = new List<object>() // Leere Liste
                };
                
                var content = new StringContent(JsonConvert.SerializeObject(shoppingCartDto), Encoding.UTF8, "application/json");
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                
                Console.WriteLine($"Sende ShoppingCart: {JsonConvert.SerializeObject(shoppingCartDto)}");
                
                var response = await _httpClient.PostAsync("ShoppingCarts", content);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<ShoppingCart>(responseContent) ?? throw new Exception("Failed to deserialize shopping cart");
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Error creating shopping cart: {errorContent}");
                    throw new HttpRequestException($"Failed to create shopping cart: {response.StatusCode} - {errorContent}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception creating shopping cart: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> AddToCartAsync(CartItem cartItem)
        {
            try
            {
                AddAuthorizationHeader();
                
                // Debug-Ausgabe für die Request-Header
                Console.WriteLine("Request-Headers für CartItem hinzufügen:");
                foreach (var header in _httpClient.DefaultRequestHeaders)
                {
                    Console.WriteLine($"{header.Key}: {string.Join(", ", header.Value)}");
                }
                
                // Erstelle ein AddToCartRequest für die API
                var addToCartRequest = new
                {
                    ProductId = cartItem.ProductId,
                    Quantity = cartItem.Quantity
                };
                
                Console.WriteLine($"Sende AddToCartRequest: {JsonConvert.SerializeObject(addToCartRequest)}");
                
                var content = new StringContent(JsonConvert.SerializeObject(addToCartRequest), Encoding.UTF8, "application/json");
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                
                // Stelle sicher, dass der Authorization-Header für jeden Request neu gesetzt wird
                AddAuthorizationHeader();
                
                var response = await _httpClient.PostAsync("ShoppingCarts/items", content);
                var responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"AddToCart Response: {response.StatusCode} - {responseContent}");
                
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception adding to cart: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return false;
            }
        }
        
        // Hilfsmethode zum Extrahieren der UserId aus dem Token
        private int GetUserIdFromToken()
        {
            try
            {
                // Token aus dem Cookie holen
                var token = _httpContextAccessor.HttpContext?.Request.Cookies["AuthToken"];
                
                if (string.IsNullOrEmpty(token) && _httpContextAccessor.HttpContext != null)
                {
                    var sessionString = _httpContextAccessor.HttpContext.Session.GetString("UserSession");
                    if (!string.IsNullOrEmpty(sessionString))
                    {
                        var userSession = JsonConvert.DeserializeObject<UserSessionModel>(sessionString);
                        return userSession?.UserId ?? 0;
                    }
                }
                
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fehler beim Extrahieren der UserId aus dem Token: {ex.Message}");
                return 0;
            }
        }

        public async Task<bool> UpdateCartItemQuantityAsync(int cartItemId, int quantity, int userId)
        {
            try
            {
                AddAuthorizationHeader();
                
                var updateRequest = new
                {
                    Quantity = quantity
                };
                
                var content = new StringContent(JsonConvert.SerializeObject(updateRequest), Encoding.UTF8, "application/json");
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                
                var response = await _httpClient.PutAsync($"ShoppingCarts/items/{cartItemId}", content);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Fehler beim Aktualisieren des Warenkorbs: {errorContent}");
                }
                
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception beim Aktualisieren des Warenkorbs: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> RemoveFromCartAsync(int cartItemId, int userId)
        {
            try
            {
                AddAuthorizationHeader();
                
                var response = await _httpClient.DeleteAsync($"ShoppingCarts/items/{cartItemId}");
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Fehler beim Entfernen aus dem Warenkorb: {errorContent}");
                }
                
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception beim Entfernen aus dem Warenkorb: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> ClearCartAsync()
        {
            try
            {
                AddAuthorizationHeader();
                
                var response = await _httpClient.DeleteAsync("ShoppingCarts");
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Fehler beim Leeren des Warenkorbs: {errorContent}");
                }
                
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception beim Leeren des Warenkorbs: {ex.Message}");
                return false;
            }
        }
    }

    // DTO-Klassen für die Kommunikation mit dem API
    public class ShoppingCartResponse
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public DateTime LastModified { get; set; }
        public List<CartItemResponse> CartItems { get; set; } = new List<CartItemResponse>();
        public int TotalItems { get; set; }
        public decimal TotalPrice { get; set; }
    }

    public class CartItemResponse
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string ProductImage { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
    }
} 