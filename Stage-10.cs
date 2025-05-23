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
      z-index: 0;
      filter: blur(4px) brightness(0.9);
    }

    .row {
      display: flex;
    }

    .row.even {
      margin-left: 128px; /* сдвиг на пол-ячейки */
    }

    .cell {
      width: 256px;
      height: 256px;
      background-image: url('logo-sngb.png');
      background-repeat: no-repeat;
      background-position: center;
      background-size: 100px;
      opacity: 0.12;
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

  <div class="background-grid" id="grid"></div>
  <div class="overlay"></div>

  <div class="card">
    <h1>Сайт временно недоступен</h1>
    <p>Проводятся технические работы. Пожалуйста, попробуйте позже.</p>
    <a href="#" onclick="location.reload()">Обновить</a>
  </div>

  <script>
    const grid = document.getElementById('grid');
    const rows = Math.ceil(window.innerHeight / 256) + 1;
    const cols = Math.ceil(window.innerWidth / 256) + 2;

    for (let i = 0; i < rows; i++) {
      const row = document.createElement('div');
      row.className = 'row' + (i % 2 === 0 ? ' even' : '');
      for (let j = 0; j < cols; j++) {
        const cell = document.createElement('div');
        cell.className = 'cell';
        row.appendChild(cell);
      }
      grid.appendChild(row);
    }
  </script>

</body>
</html>
