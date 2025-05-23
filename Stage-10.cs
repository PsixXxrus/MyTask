<!DOCTYPE html>
<html lang="ru">
<head>
  <meta charset="UTF-8" />
  <title>Сайт недоступен</title>
  <style>
    html, body {
      margin: 0;
      padding: 0;
      height: 100%;
      overflow: hidden;
      font-family: "Segoe UI", Tahoma, sans-serif;
      background-color: #f0f2f5;
    }

    .background-grid {
      position: fixed;
      top: 0; left: 0;
      width: 100vw; height: 100vh;
      display: grid;
      grid-template-columns: repeat(auto-fill, 150px);
      grid-template-rows: repeat(auto-fill, 150px);
      z-index: 0;
      filter: blur(8px) brightness(0.9);
    }

    .cell {
      width: 150px;
      height: 150px;
      background-repeat: no-repeat;
      background-position: center;
      background-size: 80px;
      opacity: 0.15;
    }

    .cell.logo {
      background-image: url('logo-sngb.png');
    }

    /* Шахматное чередование: только чётные строки и столбцы */
    .background-grid > div:nth-child(4n + 1),
    .background-grid > div:nth-child(4n + 4) {
      background-image: none;
    }

    .overlay {
      position: fixed;
      top: 0; left: 0;
      width: 100vw; height: 100vh;
      background-color: rgba(255,255,255,0.5);
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
      text-align: center;
      box-shadow: 0 0 20px rgba(0,0,0,0.1);
    }

    .card h1 {
      margin: 0 0 10px;
      color: #003366;
    }

    .card p {
      color: #333;
      font-size: 16px;
    }

    .card a {
      margin-top: 20px;
      display: inline-block;
      padding: 10px 20px;
      background: #003366;
      color: #fff;
      text-decoration: none;
      border-radius: 4px;
    }

    .card a:hover {
      background: #002244;
    }
  </style>
</head>
<body>

  <!-- Фон -->
  <div class="background-grid">
    <!-- Создаём много ячеек -->
    <script>
      for (let i = 0; i < 200; i++) {
        document.write('<div class="cell logo"></div>');
      }
    </script>
  </div>

  <div class="overlay"></div>

  <!-- Основная карточка -->
  <div class="card">
    <h1>Сайт временно недоступен</h1>
    <p>Проводятся технические работы. Пожалуйста, попробуйте позже.</p>
    <a href="#" onclick="location.reload()">Обновить</a>
  </div>

</body>
</html>
