output "url" {
  description = "HTTP do laboratório, restrito ao loopback, com porta aleatória."
  value       = module.http_demo.url
}

output "container_name" {
  value = module.http_demo.container_name
}
