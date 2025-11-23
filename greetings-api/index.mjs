import express from "express";
const app = express();

app.all("/hello", (req, res) => {
  res.status(200).json({
    message: "Hello!"
  });
});

const port = process.env.PORT || 3000;
app.listen(port, () => {
  console.log(`greetings-api running on port ${port}`);
});
