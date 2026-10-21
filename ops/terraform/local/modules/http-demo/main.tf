terraform {
  required_providers {
    docker = {
      source = "kreuzwerker/docker"
    }
  }
}

variable "name" {
  type = string
}

variable "network_name" {
  type = string
}

resource "docker_image" "busybox" {
  name         = "busybox:1.37.0"
  keep_locally = true
}

resource "docker_container" "http" {
  name  = var.name
  image = docker_image.busybox.image_id
  user  = "65534:65534"

  command = [
    "sh", "-c",
    "mkdir -p /tmp/www && printf 'FinTrack Terraform local lab\\n' > /tmp/www/index.html && exec httpd -f -p 8080 -h /tmp/www"
  ]

  read_only   = true
  tmpfs       = { "/tmp" = "rw,noexec,nosuid,size=16m,mode=1777" }
  restart     = "no"
  memory      = 32
  memory_swap = 64

  capabilities {
    drop = ["ALL"]
  }

  security_opts = ["no-new-privileges:true"]

  networks_advanced {
    name = var.network_name
  }

  ports {
    internal = 8080
    ip       = "127.0.0.1"
  }

  labels {
    label = "fintrack.managed-by"
    value = "terraform-local-lab"
  }

  healthcheck {
    test     = ["CMD", "wget", "-q", "-O", "/dev/null", "http://127.0.0.1:8080/"]
    interval = "2s"
    timeout  = "1s"
    retries  = 10
  }

  wait         = true
  wait_timeout = 30
}

output "url" {
  value = "http://127.0.0.1:${docker_container.http.ports[0].external}"
}

output "container_name" {
  value = docker_container.http.name
}
