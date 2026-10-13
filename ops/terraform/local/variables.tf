variable "docker_host" {
  description = "Socket Unix Docker local; não aceita endpoints TCP/SSH."
  type        = string
  default     = "unix:///var/run/docker.sock"

  validation {
    condition     = can(regex("^unix:///[^[:space:]]+$", var.docker_host))
    error_message = "Use somente socket Unix local (unix:///caminho/docker.sock)."
  }
}

variable "lab_name" {
  description = "Sufixo exclusivo dos recursos descartáveis; não usar nomes Compose."
  type        = string
  default     = "demo"

  validation {
    condition     = can(regex("^[a-z0-9][a-z0-9-]{0,31}$", var.lab_name))
    error_message = "Use 1 a 32 caracteres: letras minúsculas, números e hífen."
  }
}
