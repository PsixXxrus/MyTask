<!DOCTYPE html>
<html lang="ru">
<head>
  <meta charset="UTF-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1.0" />
  <title>Сайт временно недоступен</title>
  <style>
    body, html {
      margin: 0;
      padding: 0;
      height: 100%;
      font-family: "Segoe UI", Tahoma, Geneva, Verdana, sans-serif;
      background-color: #f0f2f5;
      overflow: hidden;
    }

    .background {
      position: absolute;
      top: 0; left: 0;
      width: 100%; height: 100%;
      background-image: url('logo-sngb.png');
      background-repeat: repeat;
      background-size: 150px;
      filter: blur(10px) brightness(0.9);
      z-index: 0;
    }

    .overlay {
      position: absolute;
      top: 0; left: 0;
      width: 100%; height: 100%;
      background-color: rgba(255, 255, 255, 0.6);
      z-index: 1;
    }

    .card {
      position: relative;
      z-index: 2;
      max-width: 400px;
      margin: 100px auto;
      padding: 30px;
      background: white;
      border-radius: 8px;
      box-shadow: 0 0 20px rgba(0,0,0,0.1);
      text-align: center;
    }

    .card h1 {
      margin-top: 0;
      color: #003366;
      font-size: 24px;
    }

    .card p {
      color: #444;
      font-size: 16px;
      line-height: 1.5;
    }

    .card a {
      display: inline-block;
      margin-top: 20px;
      text-decoration: none;
      color: #ffffff;
      background-color: #003366;
      padding: 10px 20px;
      border-radius: 5px;
    }

    .card a:hover {
      background-color: #002244;
    }
  </style>
</head>
<body>
  <div class="background"></div>
  <div class="overlay"></div>

  <div class="card">
    <h1>Сайт временно недоступен</h1>
    <p>
      В настоящее время ведутся технические работы или возникла непредвиденная ошибка.<br>
      Пожалуйста, попробуйте обновить страницу позже.
    </p>
    <a href="#" onclick="location.reload();">Обновить</a>
  </div>
</body>
</html>
