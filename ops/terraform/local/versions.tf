terraform {
  required_version = ">= 1.6.0, < 2.0.0"

  required_providers {
    docker = {
      source  = "kreuzwerker/docker"
      version = "4.6.0"
    }
  }

  backend "local" {
    path = "terraform.tfstate"
  }
}
