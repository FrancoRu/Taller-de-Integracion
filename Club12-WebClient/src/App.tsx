import "@fontsource/roboto/300.css";
import "@fontsource/roboto/400.css";
import "@fontsource/roboto/500.css";
import "@fontsource/roboto/700.css";

import { Route, Routes } from "react-router-dom";
import ProtectedRoute from "./pages/ProtectedRoute";
import { NavBarPage } from "./pages/navbar/NavbarPage";
import { MyForm } from "./components/form/form";

function App() {
  const handleSubmit = (event: React.FormEvent<HTMLFormElement>) => {
    // Evita que el formulario se envíe de manera predeterminada
    event.preventDefault();
    const fields = Object.fromEntries(new window.FormData(event.currentTarget));
    console.log(fields);
  };
  return (
    <>
      <h1>Hola mundo</h1>
      <MyForm handleSubmit={handleSubmit} />
    </>
  );
}

export default App;

{
  /* <NavBarPage />
			<Routes>
				<Route path="/" element={<h1>Home</h1>}></Route>
				<Route path="/login" element={<h1>Hello wordl!</h1>}></Route>
				<Route element={<ProtectedRoute />}>
					<Route path="/home" element={<MyForm />} /> 
					<Route path="/home" element={<></>} />
					<Route path="/home" element={<></>} />
					<Route path="/home" element={<></>} /> 
				</Route>
			</Routes> */
}
