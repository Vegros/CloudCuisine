import base64
import json
from google.cloud import firestore
from google.cloud import vision
import functions_framework

db = firestore.Client(database="pftc-imp-account")
vision_client = vision.ImageAnnotatorClient()

@functions_framework.cloud_event
def process_menu_upload(cloud_event):
    message = cloud_event.data["message"]

    data = base64.b64decode(message["data"]).decode("utf-8")
    payload = json.loads(data)

    restaurant_id = payload["restaurantId"]
    menu_id = payload["menuId"]
    file_name = payload["fileName"]

    image = vision.Image()
    image.source.image_uri = f"gs://menu-images-pftc/{file_name}"

    response = vision_client.document_text_detection(image=image)

    if response.error.message:
        raise Exception(response.error.message)

    ocr_text = response.full_text_annotation.text or ""

    menu_ref = (
        db.collection("restaurants")
        .document(restaurant_id)
        .collection("menus")
        .document(menu_id)
    )

    menu_snapshot = menu_ref.get()
    menu_data = menu_snapshot.to_dict() or {}
    current_status = menu_data.get("Status", "")

    update_data = {
        "OcrText": ocr_text
    }

    if current_status != "ready":
        update_data["Status"] = "pending"

    menu_ref.update(update_data)

    print(f"OCR processed for menu {menu_id}. OCR text length: {len(ocr_text)}")

    return "OK"