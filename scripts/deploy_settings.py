"""Configuracao exclusiva do deploy de Execucao."""

import secrets

IMAGE_BUILDS = {"api_image": ("oficina-mecanica-execucao", "Dockerfile")}


def new_private_secrets():
    return {"database_password": secrets.token_hex(24), "database_root_password": secrets.token_hex(24),
            "mongo_keyfile": secrets.token_hex(48)}


def service_values(common, private):
    return {
        "database_secrets": {"MONGO_INITDB_ROOT_USERNAME": "execucao_root",
                             "MONGO_INITDB_ROOT_PASSWORD": private["database_root_password"],
                             "MONGO_INITDB_DATABASE": "execucao", "MONGO_DATABASE": "execucao",
                             "MONGO_USERNAME": "execucao_app", "MONGO_PASSWORD": private["database_password"],
                             "keyfile": private["mongo_keyfile"]},
        "api_secrets": {
            "JwtSettings__SecretKey": common["jwt_secret"], "AdminUserSettings__Username": "admin",
            "AdminUserSettings__Password": common["admin_password"],
            "MongoSettings__ConnectionString": "mongodb://execucao_app:" + private["database_password"]
                + "@mongo.oficina-execucao.svc.cluster.local:27017/execucao?authSource=execucao&replicaSet=rs0&directConnection=true",
        },
    }
