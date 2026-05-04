import re
import functions_framework
from google.cloud import firestore

db = firestore.Client(database="pftc-imp-account")


BLACKLIST_TERMS = [
    "available",
    "gf",
    "gf/v",
    "vegan",
    "vegetarian",
    "gluten free",
    "service charge",
    "allergies",
    "allergy",
    "intolerance",
    "instagram",
    "tiktok",
    "email",
    "phone",
    "road",
    "london",
    "menu",
    "lunch",
    "dinner",
    "sides",
    "food allergies",
    "before ordering",
    "speak to",
    "member of staff",
    "service charge",
]


def is_bad_menu_item(name):
    cleaned = name.lower().strip()

    if len(cleaned) < 3:
        return True

    exact_blacklist = {
        "gf",
        "v",
        "gf/v",
        "available gf/v +",
        "available gf/v",
        "available gf/v + £1.99",
        "sides",
        "lunch & dinner",
        "lunch",
        "dinner",
    }

    if cleaned in exact_blacklist:
        return True

    if any(term in cleaned for term in BLACKLIST_TERMS):
        return True

    letters = sum(c.isalpha() for c in cleaned)
    if letters < 3:
        return True

    return False


def parse_menu_items(ocr_text):
    items = []
    lines = [line.strip() for line in ocr_text.splitlines() if line.strip()]

    for i, line in enumerate(lines):
        match = re.search(r"[€£]\s?(\d+[.,]\d{2})", line)

        if not match:
            continue

        price = float(match.group(1).replace(",", "."))
        name = line.replace(match.group(0), "").strip()

        if not name and i > 0:
            name = lines[i - 1].strip()

        if not name:
            continue

        if re.match(r"^[€£]?\d+[.,]\d{2}$", name):
            continue

        if is_bad_menu_item(name):
            continue

        items.append({
            "Name": name,
            "Price": price
        })

    return items



@functions_framework.http
def clean_pending_menus(request):
    restaurants_ref = db.collection("restaurants")
    restaurants = restaurants_ref.stream()

    processed_menus = 0
    processed_restaurants = 0
    total_items = 0

    for restaurant in restaurants:
        restaurant_id = restaurant.id
        restaurant_processed = False

        menus_ref = (
            db.collection("restaurants")
            .document(restaurant_id)
            .collection("menus")
        )

        pending_menus = menus_ref.where("Status", "==", "pending").stream()

        for menu in pending_menus:
            menu_id = menu.id
            menu_data = menu.to_dict()

            ocr_text = menu_data.get("OcrText", "")

            if not ocr_text:
                continue

            parsed_items = parse_menu_items(ocr_text)

            menu_ref = menus_ref.document(menu_id)

            old_items = menu_ref.collection("items").stream()
            for old_item in old_items:
                old_item.reference.delete()

            for item in parsed_items:
                item_ref = menu_ref.collection("items").document()
                item_ref.set({
                    "ItemId": item_ref.id,
                    "Name": item["Name"],
                    "Price": item["Price"],
                    "CreatedAt": firestore.SERVER_TIMESTAMP
                })

            menu_ref.update({
                "Status": "ready"
            })

            processed_menus += 1
            total_items += len(parsed_items)
            restaurant_processed = True

        if restaurant_processed:
            restaurant.reference.update({
                "Status": "ready"
            })
            processed_restaurants += 1

    return ({
                "processedRestaurants": processed_restaurants,
                "processedMenus": processed_menus,
                "createdItems": total_items
            }, 200)