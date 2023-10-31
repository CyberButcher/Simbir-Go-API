namespace Simbir_GO_Api.Middleware
{
    public class JwtBanMiddleware
    {
        private readonly RequestDelegate _next;
        private MyDBContext _dbContext = new MyDBContext();

        public JwtBanMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            var jwtToken = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();

            if (IsTokenBanned(jwtToken))
            {
                context.Response.StatusCode = 401; // 401 Unauthorized
                await context.Response.WriteAsync("Unauthorized: Your token is unavailable.");
                return;
            }

            await _next(context);
        }

        private bool IsTokenBanned(string token)
        {
            bool isTokenBanned = false;
            _dbContext.BannedJWTs.ToList().ForEach(j =>
            {
                if (j.Token == token)
                {
                    isTokenBanned = true;
                }
            });

            return isTokenBanned;
        }
    }
}
