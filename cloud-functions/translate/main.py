import os
import redis
import functions_framework
from google.cloud import translate_v2 as translate

translate_client = translate.Client()

redis_client = redis.Redis(
    host=os.environ["REDIS_HOST"],
    port=int(os.environ["REDIS_PORT"]),
    username=os.environ["REDIS_USERNAME"],
    password=os.environ["REDIS_PASSWORD"],
    ssl=False,
    decode_responses=True
)

@functions_framework.http
def translate_text(request):
    if request.method == "OPTIONS":
        return ("", 204, {
            "Access-Control-Allow-Origin": "*",
            "Access-Control-Allow-Methods": "POST, OPTIONS",
            "Access-Control-Allow-Headers": "Content-Type",
        })

    headers = {"Access-Control-Allow-Origin": "*"}

    if request.path == "/clear_cache":
        data = request.get_json(silent=True)

        if not data or "restaurantId" not in data:
            return ({"error": "Missing restaurantId"}, 400, headers)

        restaurant_id = data["restaurantId"]
        keys = redis_client.keys(f"translation:{restaurant_id}:*")

        if keys:
            redis_client.delete(*keys)

        return ({
                    "message": "Cache cleared",
                    "deletedKeys": len(keys)
                }, 200, headers)

    data = request.get_json(silent=True)

    if not data or "text" not in data:
        return ({"error": "Missing text"}, 400, headers)

    text = data["text"]
    target = data.get("target", "en")
    restaurant_id = data.get("restaurantId", "global")

    cache_key = f"translation:{restaurant_id}:{target}:{text.lower()}"

    cached = redis_client.get(cache_key)

    if cached:
        return ({
                    "translatedText": cached,
                    "source": "cache"
                }, 200, headers)

    result = translate_client.translate(text, target_language=target)
    translated = result["translatedText"]

    redis_client.set(cache_key, translated, ex=86400)

    return ({
                "translatedText": translated,
                "source": "api"
            }, 200, headers)