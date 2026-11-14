locals {
  service_settings = { MongoSettings__DatabaseName = "execucao" }
  mongo_scripts = {
    "mongo-init.js"         = file("${path.module}/../../k8s/mongo-init.js")
    "mongo-replica-init.js" = file("${path.module}/../../k8s/mongo-replica-init.js")
  }
  bootstrap_template = merge(local.api_template, {
    job_name = "mongo-init-${substr(nonsensitive(sha256(jsonencode([var.database_secrets, local.mongo_scripts]))), 0, 12)}"
  })
}

resource "kubernetes_persistent_volume_claim_v1" "database" {
  metadata {
    name      = "database-data"
    namespace = kubernetes_namespace_v1.service.metadata[0].name
  }
  spec {
    access_modes       = ["ReadWriteOnce"]
    storage_class_name = "standard"
    resources { requests = { storage = "1Gi" } }
  }
  wait_until_bound = false
  lifecycle { prevent_destroy = true }
}

resource "kubernetes_secret_v1" "database" {
  metadata {
    name      = "database-secrets"
    namespace = kubernetes_namespace_v1.service.metadata[0].name
  }
  data = var.database_secrets
}

resource "kubernetes_config_map_v1" "mongo" {
  metadata {
    name      = "mongo-bootstrap"
    namespace = kubernetes_namespace_v1.service.metadata[0].name
  }
  data = local.mongo_scripts
}

resource "kubernetes_manifest" "database_service" {
  manifest   = yamldecode(templatefile("${path.module}/../../k8s/mongo-service.yaml.tftpl", local.api_template))
  depends_on = [kubernetes_namespace_v1.service]
}

resource "kubernetes_manifest" "database" {
  manifest = yamldecode(templatefile("${path.module}/../../k8s/mongo-statefulset.yaml.tftpl", local.api_template))
  depends_on = [kubernetes_persistent_volume_claim_v1.database, kubernetes_secret_v1.database,
  kubernetes_config_map_v1.mongo, kubernetes_manifest.database_service]
  wait { rollout = true }
  timeouts {
    create = "10m"
    update = "10m"
  }
}

resource "kubernetes_manifest" "database_init" {
  computed_fields = ["metadata.annotations", "metadata.labels", "spec.template.metadata.labels"]
  manifest        = yamldecode(templatefile("${path.module}/../../k8s/mongo-init-job.yaml.tftpl", local.bootstrap_template))
  depends_on      = [kubernetes_manifest.database]
  wait {
    condition {
      type   = "Complete"
      status = "True"
    }
  }
  timeouts {
    create = "8m"
    update = "8m"
  }
}
