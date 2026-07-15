# Deployment

## Hugging Face Space (free demo hosting)

The `huggingface/` folder is a complete Docker Space definition that builds
the API from the public GitHub repository and runs it together with
PostgreSQL in a single free-tier container.

To deploy:

1. Make sure the GitHub repository is public (the Space clones it at build time).
2. Create a new Space at https://huggingface.co/new-space — pick **Docker** as the SDK.
3. Upload the three files from `deploy/huggingface/` (`README.md`, `Dockerfile`, `start.sh`)
   to the Space, keeping the names. The web editor's "Add file" button is enough.

The Space builds for a few minutes, then serves Swagger as its landing page.
Data is ephemeral and reseeds on every restart, which suits a demo.

## Anywhere else

`Dockerfile` + `docker-compose.yml` at the repository root run the full
stack (API, PostgreSQL, Blazor UI) on any Docker host. Supply real values
for `JwtSettings__SecretKey` and the connection string via environment
variables for non-demo use.
