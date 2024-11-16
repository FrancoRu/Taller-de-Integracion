// import axios from "axios";
// import { useState } from "react";
// // import { toast } from 'react-toastify'

// const useApiRequest = () => {
//   const [loading, setLoading] = useState(false);
//   const [error] = useState();

//   const sendRequest = async (
//     method: any,
//     url: any,
//     fields: any,
//     token: any
//   ) => {
//     try {
//       setLoading(true);
//       const urlConstructed = import.meta.env.VITE_APP_REST_API_DOMAIN + url;

//       const headers = {
//         Authorization: `Bearer ${token}`,
//         //"Content-Type": "application/x-www-form-urlencoded",
//         "Content-Type": "application/json",
//         Accept: "application/x-www-form-urlencoded",
//       };

//       const response = await axios({
//         method,
//         url: urlConstructed,
//         data: fields,
//         headers,
//       });
//       debugger;
//       if (response.status >= 200 && response.status < 300) {
//         // Successful response
//         setLoading(false);
//         return response.data;
//       } else {
//         setLoading(false);
//         // Handle unsuccessful response (optional)
//         //setError("Unsuccessful response");
//         throw new Error("Unsuccessful response");
//       }
//     } catch (error) {
//       // toast.error(`Error: ${error?.response.data.error}`)
//       // toast.error(`Error: ${error?.response.data.mensaje}`)
//       setLoading(false);
//       //setError(error?.response?.data?.error);
//       throw new Error("Error in the API request");
//     }
//   };

//   return { sendRequest, loading, error };
// };

// export default useApiRequest;
