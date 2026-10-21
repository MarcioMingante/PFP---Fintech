provider "docker" {
  host    = var.docker_host
  context = ""
}

locals {
  prefix = "fintrack-iac-${var.lab_name}"
}

resource "docker_network" "lab" {
  name = "${local.prefix}-network"

  labels {
    label = "fintrack.managed-by"
    value = "terraform-local-lab"
  }
}

module "http_demo" {
  source = "./modules/http-demo"

  name         = "${local.prefix}-http"
  network_name = docker_network.lab.name
}
