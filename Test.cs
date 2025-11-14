Есть инструкция по загрузки файлов на yandex.cloud
https://yandex.cloud/ru/docs/storage/s3/s3-api-quickstart#curl-821_2

Для загрузки используется запрос curl
curl \
  --request PUT \
  --upload-file "${LOCAL_FILE}" \
  --verbose \
  --header "Host: storage.yandexcloud.net" \
  --header "Date: ${DATE_VALUE}" \
  --header "Authorization: AWS ${AWS_KEY_ID}:${SIGNATURE}" \
  "https://storage.yandexcloud.net/${BUCKET_NAME}/${OBJECT_PATH}"
А в результате я должен получить
< HTTP/2 200
< server: nginx
< date: Thu, 15 May 2025 07:23:08 GMT
< content-type: text/plain
< etag: "f75a361db63aa4722fb8e083********"
< x-amz-request-id: 67ccce91********
<
* Connection #0 to host storage.yandexcloud.net left intact

Мне нужно, чтобы ты переписал это на HTTP в C#, но авторизацию я буду использовать не AWS а Api-Key
